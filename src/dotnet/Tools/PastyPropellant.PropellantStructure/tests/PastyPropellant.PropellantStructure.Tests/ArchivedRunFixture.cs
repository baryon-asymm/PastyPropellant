using System.Diagnostics;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Sampling;
using PastyPropellant.PropellantStructure.Results;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// The archived runs L2 replays, each executed once and shared by every test that
/// reads it.
/// </summary>
/// <remarks>
/// <para>
/// The four L2 groups — draw counters, condition counters, scalars, arrays — are
/// separate tests on purpose: they fail for different reasons and the order in which
/// they are read is part of the method (see <c>Kernel/BOOT.md</c>). But they are four
/// readings of the <b>same</b> runs, and letting each construct its own turned ten
/// minutes of Monte Carlo into forty for no extra evidence: a run is deterministic, so
/// the copies were identical by construction.
/// </para>
/// <para>
/// ⚠ The replay hands out <see cref="StructureResult"/> and nothing else. It could
/// have carried the raw <see cref="RunState"/> too, and the first draft did — but that
/// state is mutable, so a test that wrote to it would silently change what the next
/// test sees, and the failure would land on whichever test xUnit happened to schedule
/// second. Everything L2 needs is in the result, the draw counters included.
/// </para>
/// </remarks>
public sealed class ArchivedRunFixture
{
    private readonly Dictionary<string, ArchivedRunReplay> _replays = new(StringComparer.Ordinal);

    /// <summary>
    /// The cheapest archived run whose <c>.dat</c> still describes it — the one a
    /// single-run test uses.
    /// </summary>
    /// <remarks>
    /// ⚠ Cheapness is not the only criterion, and it is not the first one. The
    /// genuinely cheapest run, <c>hp2</c>, has an input file that was edited after the
    /// run — a different recipe entirely — so only the report's three-significant-digit
    /// header survives, and three digits of a mass fraction do not fix a draw sequence.
    /// A run without <c>HasExactInput</c> cannot be compared on integers at all.
    /// </remarks>
    public const string RunId = "hp1";

    /// <summary>
    /// Every archived run L2 replays in full: exact input, and small enough to run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Twenty-one of the forty-three archived runs have an input file that still agrees
    /// with the report they produced. They split sharply by cost: these six are 5 000
    /// to 10 000 base particles, and the other fifteen are 100 000 to 1 000 000 — an
    /// hour and a half to half a day each. Six of them cost about what one of the
    /// others would.
    /// </para>
    /// <para>
    /// ⚠ Two of the six are here for coverage rather than cost. <c>rps01</c> and
    /// <c>rps02</c> are the only exact-input runs that set a pocket-forming-fraction
    /// mask, so without them that branch is replayed by nothing. The rest of the
    /// archive is one corner of the parameter space — <c>KXX = 1</c>, <c>gsv = 2</c>,
    /// <c>ivar = 0</c>, <c>SFR = 0</c>, law 1 in 39 runs of 43 — and no number of
    /// archived runs will cover the branches outside it. Those need fixtures generated
    /// under wine; see <c>reference/propstruct/README.md</c>.
    /// </para>
    /// </remarks>
    public static TheoryData<string> ReplayableRuns { get; } =
        ["rps02", "rps01", "hp1", "hp315", "r1", "r2", "hp1050"];

    /// <summary>Replays one run, or returns the replay already made.</summary>
    public ArchivedRunReplay For(string id)
    {
        lock (_replays)
        {
            if (_replays.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var run = ReferenceRuns.ById(id);
            if (!run.HasExactInput)
            {
                throw new InvalidOperationException(
                    $"{id} has no exact input, so nothing about it can be compared. Pick another run.");
            }

            var simulation = new StructureSimulation(run.ToConfiguration(), RandomStreamSet.Historical());
            var stopwatch = Stopwatch.StartNew();
            var result = simulation.Run();
            var replay = new ArchivedRunReplay(run, result, stopwatch.Elapsed);

            _replays[id] = replay;
            return replay;
        }
    }
}

/// <summary>One archived run, replayed.</summary>
/// <param name="Run">what the archive says the run produced</param>
/// <param name="Result">what the port produced, packed as it would be for a consumer</param>
/// <param name="Elapsed">how long it took, so the cost of the suite stays visible</param>
public sealed record ArchivedRunReplay(
    ReferenceRun Run,
    StructureResult Result,
    TimeSpan Elapsed);
