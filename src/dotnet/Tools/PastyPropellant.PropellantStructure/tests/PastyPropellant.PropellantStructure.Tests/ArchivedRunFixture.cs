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
    /// <b>Twenty</b> archived runs whose input file still agrees with the report it
    /// produced — every <see cref="ReferenceRun.HasExactInput"/> report there is, now
    /// that the flag counts mass shares as well as size bounds (see below). There is no
    /// cost filter, and the one that used to be
    /// here was measured against the wrong program: the note this replaces put the
    /// fifteen large runs at "an hour and a half to half a day each", which is what the
    /// <i>original</i> costs. The port is two to three orders faster — <c>hp1</c> draws 101 million
    /// pockets in about two minutes — so the whole set fits in one long-running suite.
    /// </para>
    /// <para>
    /// ⚠ Cost was never the real argument anyway, and the reason to run all of them is
    /// coverage of <em>recipes</em>. The seven that used to be here are one corner of the
    /// input space: two or three fractions, oxidiser 0.583, sizes 10-315 µm. The
    /// thirteen added reach fourteen fractions and 50-700 µm (<c>rt56</c>), a
    /// five-fraction narrow cut (<c>rcspx01</c>, <c>rnano2</c>), oxidiser 0.489-0.689,
    /// metal 0.150-0.247, an eta of 0.5 (<c>p777out1</c>) and a million base particles
    /// (<c>r_p35050nn</c>). A port trusted to predict an <i>unmeasured</i> recipe has to
    /// be checked on more than one recipe shape.
    /// </para>
    /// <para>
    /// ⚠ <c>hp90</c> is <b>excluded</b>. <c>hp90</c> and <c>hp95</c> both name
    /// <c>HPEPA10.dat</c>, and the file survives only in its last state — the
    /// <c>hp95</c> recipe. The archive's own check missed it: <c>check_dat_against_m</c>
    /// compares each fraction's size bounds and never its mass share, and the two runs
    /// share the bounds (10-50, 113-180, 160-315 µm). So the fixture handed the port
    /// <c>hp95</c>'s recipe under <c>hp90</c>'s name and compared it against
    /// <c>hp90</c>'s report — and the giveaway was that the two runs then produced the
    /// same <c>Nkarm</c> to the unit, 27 148 392, for supposedly different recipes.
    /// <see cref="ReferenceInputFile.SharesMatchReport"/> now completes the comparison,
    /// so this exclusion is enforced rather than remembered: <c>hp90</c> no longer has
    /// an exact input and <see cref="For"/> would refuse it. Of the forty-three files
    /// fourteen disagree on shares and thirteen were already flagged on bounds, so
    /// completing the check moved this one run and no other — see
    /// <see cref="ReferenceRunsTests"/>.
    /// </para>
    /// <para>
    /// Nor can the report supply the input in the <c>.dat</c>'s place: <c>Gfr</c> is
    /// printed as <c>E9.3</c>, three significant digits, and the run needs four. The
    /// sweep <c>hp90</c> belongs to sets the fine share to <c>x·0.514</c>, so the recipe
    /// is 0.4626/0.0514 and the report rounds it to 0.463/0.0514. Replaying the rounded
    /// form misses <c>Nkarm</c> by 25 838 of 30 million; the unrounded one lands within
    /// two. That number is a reconstruction of the design rule rather than an archived
    /// record, which is exactly why it does not belong in a golden-master list — but it
    /// does mean the <c>hp90</c> failure was never the port's.
    /// </para>
    /// <para>
    /// ⚠ <c>rps01</c> and <c>rps02</c> remain the only exact-input runs that set a
    /// pocket-forming-fraction mask. What no archived run covers at all is
    /// <c>KXX &gt; 1</c>, <c>gsv != 2</c>, <c>ivar != 0</c> and law 2 — 39 of the 43 sit
    /// at one setting of each — and no number of archived runs will change that. Those
    /// need fixtures generated under wine; see <c>reference/propstruct/README.md</c>.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> ReplayableRunIds { get; } =
    [
        // the original seven, cheapest first
        "rps02", "rps01", "hp1", "hp315", "r1", "r2", "hp1050",
        // the thirteen the stale cost estimate kept out (hp90 is the fourteenth and is
        // deliberately absent - its .dat is a later revision, see the remarks)
        "hp180", "r4", "rcspx01", "rnano2", "results", "testtest", "hp95", "rt56",
        "p777out", "p777out1", "p777out2", "res_01", "r_p35050nn",
    ];

    /// <summary>
    /// The same list in the shape <c>[MemberData]</c> wants. It is derived rather than
    /// written twice: a second literal list is a second thing to forget to update, and
    /// <see cref="KnownDeviations"/> is checked against the plain one.
    /// </summary>
    public static TheoryData<string> ReplayableRuns { get; } = [.. ReplayableRunIds];

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
