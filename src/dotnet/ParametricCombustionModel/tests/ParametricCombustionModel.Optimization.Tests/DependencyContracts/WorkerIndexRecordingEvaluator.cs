using DotNetOptimization.Abstractions;

namespace ParametricCombustionModel.Optimization.Tests.DependencyContracts;

/// <summary>
/// A stub <see cref="IFitnessFunctionEvaluator"/> that records how the DE library drives it,
/// rather than computing anything the optimiser cares about.
///
/// It exists to observe one thing: whether two evaluations that are *in flight at the same
/// moment* are ever handed the same <c>workerIndex</c>. It detects that with a per-index busy
/// flag flipped via <see cref="Interlocked.Exchange(ref int, int)"/> — if a call finds the flag
/// for its own index already set, some other thread is inside the same index right now.
/// </summary>
internal sealed class WorkerIndexRecordingEvaluator : IFitnessFunctionEvaluator
{
    private readonly int _declaredWorkerCount;
    private readonly int[] _busy;
    private readonly int[] _callsPerIndex;

    private int _concurrentSharingViolations;
    private int _outOfRangeCalls;
    private int _indexedCalls;
    private int _nonIndexedCalls;
    private int _worstObservedIndex = int.MinValue;

    private int _nonIndexedBusy;
    private int _nonIndexedConcurrentCalls;
    private readonly HashSet<int> _nonIndexedThreadIds = [];

    public WorkerIndexRecordingEvaluator(int declaredWorkerCount)
    {
        _declaredWorkerCount = declaredWorkerCount;
        _busy = new int[declaredWorkerCount];
        _callsPerIndex = new int[declaredWorkerCount];
    }

    /// <summary>Number of evaluations where a second thread held the same worker index concurrently.</summary>
    public int ConcurrentSharingViolations => Volatile.Read(ref _concurrentSharingViolations);

    /// <summary>Number of evaluations whose worker index fell outside <c>[0, declaredWorkerCount)</c>.</summary>
    public int OutOfRangeCalls => Volatile.Read(ref _outOfRangeCalls);

    /// <summary>Calls that arrived on the worker-indexed overload.</summary>
    public int IndexedCalls => Volatile.Read(ref _indexedCalls);

    /// <summary>Calls that arrived on the index-free overload.</summary>
    public int NonIndexedCalls => Volatile.Read(ref _nonIndexedCalls);

    /// <summary>The largest out-of-range index actually seen, for diagnostics. <see cref="int.MinValue"/> if none.</summary>
    public int WorstObservedIndex => Volatile.Read(ref _worstObservedIndex);

    /// <summary>How many distinct worker indices the library actually used.</summary>
    public int DistinctIndicesUsed => _callsPerIndex.Count(c => c > 0);

    public IReadOnlyList<int> CallsPerIndex => _callsPerIndex;

    /// <summary>
    /// The index-free overload. Production's <c>GroupDifferentialEvolutionOptimizer.Evaluate(genes)</c>
    /// forwards to worker 0 unconditionally, so if the library ever drove evaluation through *this*
    /// overload on multiple threads, every thread would share worker 0's mutable contexts.
    /// Counted, not thrown on, so the test can report exactly what the library did.
    /// </summary>
    public double Evaluate(ReadOnlySpan<double> genes)
    {
        Interlocked.Increment(ref _nonIndexedCalls);

        lock (_nonIndexedThreadIds)
            _nonIndexedThreadIds.Add(Environment.CurrentManagedThreadId);

        var previouslyBusy = Interlocked.Exchange(ref _nonIndexedBusy, 1);
        if (previouslyBusy != 0)
            Interlocked.Increment(ref _nonIndexedConcurrentCalls);

        try
        {
            var result = Sphere(genes);
            Thread.SpinWait(200);
            return result;
        }
        finally
        {
            if (previouslyBusy == 0)
                Interlocked.Exchange(ref _nonIndexedBusy, 0);
        }
    }

    /// <summary>Index-free calls that overlapped another index-free call in time.</summary>
    public int NonIndexedConcurrentCalls => Volatile.Read(ref _nonIndexedConcurrentCalls);

    /// <summary>Distinct managed threads that entered the index-free overload.</summary>
    public int NonIndexedThreadCount
    {
        get { lock (_nonIndexedThreadIds) return _nonIndexedThreadIds.Count; }
    }

    public double Evaluate(int workerIndex, ReadOnlySpan<double> genes)
    {
        Interlocked.Increment(ref _indexedCalls);

        if (workerIndex < 0 || workerIndex >= _declaredWorkerCount)
        {
            Interlocked.Increment(ref _outOfRangeCalls);
            RecordWorstIndex(workerIndex);
            return Sphere(genes);
        }

        Interlocked.Increment(ref _callsPerIndex[workerIndex]);

        // Claim this index. A non-zero previous value means another thread is inside it right now.
        var previouslyBusy = Interlocked.Exchange(ref _busy[workerIndex], 1);
        if (previouslyBusy != 0)
            Interlocked.Increment(ref _concurrentSharingViolations);

        try
        {
            // Hold the claim briefly so a genuine collision has a realistic window to be observed.
            // Without this the critical section is so short that a broken library could slip through.
            var result = Sphere(genes);
            Thread.SpinWait(200);
            return result;
        }
        finally
        {
            // Release only if we were the legitimate holder; on a violation the other thread's
            // release would otherwise clear a flag it never set.
            if (previouslyBusy == 0)
                Interlocked.Exchange(ref _busy[workerIndex], 0);
        }
    }

    private void RecordWorstIndex(int index)
    {
        int seen;
        do
        {
            seen = Volatile.Read(ref _worstObservedIndex);
            if (seen != int.MinValue && Math.Abs(seen) >= Math.Abs(index))
                return;
        }
        while (Interlocked.CompareExchange(ref _worstObservedIndex, index, seen) != seen);
    }

    private static double Sphere(ReadOnlySpan<double> genes)
    {
        var sum = 0.0;
        foreach (var g in genes)
            sum += g * g;
        return sum;
    }
}
