using DotNetDifferentialEvolution;
using DotNetDifferentialEvolution.TerminationStrategies;

namespace ParametricCombustionModel.Optimization.Tests.DependencyContracts;

/// <summary>
/// Characterises the behaviour of <b>DotNetDifferentialEvolution 5.1.0</b> that this codebase's
/// concurrency scheme is built on. These are not tests of our own logic — they assert what the
/// third-party package does, so that a deliberate version bump is a checked change rather than a
/// blind one (tech-debt TEST-7).
///
/// The contract under test: <c>UseProcessors(n)</c> must drive
/// <c>IFitnessFunctionEvaluator.Evaluate(workerIndex, genes)</c> such that
/// <list type="bullet">
///   <item>every <c>workerIndex</c> lies in <c>[0, n)</c>, and</item>
///   <item>no two evaluations running at the same moment ever receive the same <c>workerIndex</c>.</item>
/// </list>
///
/// Why it is load-bearing: <c>GroupDifferentialEvolutionScenario</c> pre-allocates
/// <c>OptimizationProblemByDoubles[processorCount, 3]</c> and
/// <c>GroupDifferentialEvolutionOptimizer.Evaluate</c> selects its scratch state with
/// <c>_compositionProblems[workerIndex, groupIdx]</c>. Those contexts are <b>mutated in place</b>
/// during a solve. The worker index is the *only* thing keeping two threads out of the same
/// context. If a future library version reuses or collides indices, nothing throws and nothing
/// fails to compile — the optimisation simply produces quietly wrong numbers.
/// </summary>
public class DifferentialEvolutionWorkerIndexContractTests
{
    private const int GenomeSize = 6;
    private const int PopulationSize = 48;
    private const int Generations = 40;

    /// <summary>
    /// Worker counts to exercise. PopulationSize is divisible by each, matching the production
    /// constraint that the processor count is reduced until it divides the population evenly.
    /// </summary>
    public static TheoryData<int> WorkerCounts => new() { 2, 3, 4 };

    [Theory]
    [MemberData(nameof(WorkerCounts))]
    public async Task UseProcessors_NeverHandsTheSameWorkerIndexToTwoConcurrentEvaluations(int workerCount)
    {
        var evaluator = new WorkerIndexRecordingEvaluator(workerCount);

        await RunShortOptimisationAsync(evaluator, workerCount);

        Assert.True(
            evaluator.IndexedCalls > 0,
            "DotNetDifferentialEvolution never called the worker-indexed Evaluate(int, ReadOnlySpan<double>) "
            + "overload. The whole per-worker context scheme assumes it does; if the library switched to the "
            + "index-free overload, GroupDifferentialEvolutionOptimizer.Evaluate would route every thread to "
            + "worker 0 and all threads would share one mutable OptimizationProblemByDoubles.");

        Assert.True(
            evaluator.OutOfRangeCalls == 0,
            $"DotNetDifferentialEvolution passed a workerIndex outside [0, {workerCount}) after "
            + $"UseProcessors({workerCount}). Worst index seen: {evaluator.WorstObservedIndex}. "
            + "GroupDifferentialEvolutionOptimizer indexes _compositionProblems[workerIndex, groupIdx] with "
            + "this value, so an out-of-range index is at best an IndexOutOfRangeException mid-run and at "
            + "worst a silently wrong context. Do not ship this library version until the indexing contract "
            + "is re-established.");

        Assert.True(
            evaluator.ConcurrentSharingViolations == 0,
            $"DotNetDifferentialEvolution ran {evaluator.ConcurrentSharingViolations} evaluation(s) that shared a "
            + $"workerIndex with another evaluation already in flight (UseProcessors({workerCount})).\n\n"
            + "THIS SILENTLY CORRUPTS RESULTS. GroupDifferentialEvolutionOptimizer.Evaluate uses workerIndex to "
            + "pick its OptimizationProblemByDoubles from the [processorCount, 3] matrix, and those contexts are "
            + "MUTATED IN PLACE during a solve. Two threads inside the same context interleave their writes: no "
            + "exception is thrown, no test of our own code fails, and the optimiser reports fitness values "
            + "computed from a torn mixture of two candidate vectors.\n\n"
            + "If you are seeing this after a package upgrade: STOP the upgrade. Either the new version must "
            + "restore the exclusive-worker-index guarantee, or GroupDifferentialEvolutionScenario must switch "
            + "from pre-allocated per-worker contexts to per-evaluation allocation (slower) or thread-local "
            + "contexts keyed on something other than workerIndex.");

        Assert.True(
            evaluator.DistinctIndicesUsed > 1,
            $"UseProcessors({workerCount}) only ever used {evaluator.DistinctIndicesUsed} distinct worker index/indices "
            + $"(calls per index: [{string.Join(", ", evaluator.CallsPerIndex)}]). The no-collision assertion above is "
            + "then vacuous — a single-threaded run trivially satisfies it. Either the library stopped honouring the "
            + "requested processor count, or it silently fell back to sequential evaluation, which would make the "
            + "production run far slower than intended without any visible error.");
    }

    /// <summary>
    /// Pins how the library uses the <b>index-free</b> overload.
    ///
    /// Measured behaviour, unchanged from 4.0.0 through 5.1.0: the library calls <c>Evaluate(genes)</c> exactly
    /// <c>PopulationSize</c> times — the initial population evaluation — and does so
    /// <b>sequentially, on a single thread</b>, before any worker threads start. Every subsequent
    /// evaluation goes through the worker-indexed overload.
    ///
    /// That sequencing is the only reason production is safe today. Production's index-free
    /// overload forwards to <c>workerIndex 0</c> unconditionally, so if the library ever
    /// parallelised the initial evaluation, <c>PopulationSize</c> threads would pile into worker
    /// 0's mutable <c>OptimizationProblemByDoubles</c> at once. This test is what would catch that.
    /// </summary>
    [Fact]
    public async Task InitialPopulationEvaluation_UsesTheIndexFreeOverloadSequentiallyOnOneThread()
    {
        const int workerCount = 4;
        var evaluator = new WorkerIndexRecordingEvaluator(workerCount);

        await RunShortOptimisationAsync(evaluator, workerCount);

        Assert.Equal(PopulationSize, evaluator.NonIndexedCalls);

        Assert.True(
            evaluator.NonIndexedConcurrentCalls == 0,
            $"DotNetDifferentialEvolution ran {evaluator.NonIndexedConcurrentCalls} overlapping call(s) to the "
            + "index-free Evaluate(ReadOnlySpan<double>) overload.\n\n"
            + "THIS SILENTLY CORRUPTS RESULTS. GroupDifferentialEvolutionOptimizer implements that overload as "
            + "Evaluate(workerIndex: 0, genes) — it has no worker index to work with — so concurrent calls all "
            + "mutate worker 0's OptimizationProblemByDoubles contexts simultaneously. Through 5.1.0 this was safe only "
            + "because the initial population was evaluated sequentially before the worker threads started.\n\n"
            + "If you are seeing this after a package upgrade: the initial-population evaluation has been "
            + "parallelised. Production must stop routing the index-free overload to a shared context before this "
            + "version can ship.");

        Assert.True(
            evaluator.NonIndexedThreadCount == 1,
            $"The index-free overload was entered from {evaluator.NonIndexedThreadCount} distinct threads (expected 1). "
            + "Even without a detected overlap, multiple threads reaching an overload that production pins to "
            + "worker 0 means the sequential-setup assumption above no longer holds. Re-verify before upgrading.");
    }

    private static async Task RunShortOptimisationAsync(WorkerIndexRecordingEvaluator evaluator, int workerCount)
    {
        var lowerBound = Enumerable.Repeat(-5.0, GenomeSize).ToArray();
        var upperBound = Enumerable.Repeat(5.0, GenomeSize).ToArray();

        // jDE mirrors the production group run's strategy (see GroupScenarioRunner).
        using var de = DifferentialEvolutionBuilder
            .ForFunction(evaluator)
            .WithBounds(lowerBound, upperBound)
            .WithPopulationSize(PopulationSize)
            .WithUniformPopulationSampling()
            // Note: the adaptive strategies (jDE/JADE/SHADE/L-SHADE) return ITerminationConditionRequired
            // directly — they bake in their own selection strategy, so WithDefaultSelectionStrategy() is
            // neither required nor available after them. That staging is itself part of the API contract:
            // if an upgrade changes it, this file stops compiling, which is the cheap kind of breakage.
            .WithJde(0.1, 0.1)
            .WithTerminationCondition(new LimitGenerationNumberTerminationStrategy(Generations))
            .UseProcessors(workerCount)
            .Build();

        await de.RunAsync();
    }
}
