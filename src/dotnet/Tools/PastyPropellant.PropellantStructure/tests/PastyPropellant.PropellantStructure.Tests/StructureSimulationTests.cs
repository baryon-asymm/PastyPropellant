using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Results;
using UnitsNet;
using PastyPropellant.PropellantStructure.Sampling;
using Xunit.Abstractions;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// L2, first group: the integer counters. Nothing here has a tolerance — a draw
/// counter that is off by one means the port took a different path, and averaging it
/// away would hide exactly what the counter exists to show.
/// </summary>
/// <remarks>
/// ⚠ The order of comparison is fixed by <c>BOOT.md</c>: integer counters, then the
/// nine condition counters, then scalars, then arrays. Reading a discrepancy in the
/// later groups while the earlier ones disagree tells you nothing.
/// </remarks>
public sealed class StructureSimulationTests(ITestOutputHelper output, ArchivedRunFixture fixture)
    : IClassFixture<ArchivedRunFixture>
{

    /// <summary>
    /// The smallest normal single. Below it the original is printing memory it never
    /// wrote, not a computed number.
    /// </summary>
    private const double Denormal = 1.1754944e-38;

    [Fact]
    public void APreparatoryPassRunsBeforeEveryWorkingOne()
    {
        var simulation = new StructureSimulation(
            ReferenceRuns.ById(ArchivedRunFixture.RunId).ToConfiguration(), RandomStreamSet.Historical());

        Assert.Equal(2, simulation.TotalPasses);
    }

    /// <summary>
    /// The neighbour loop's exit threshold is the REAL*4 fold of <c>12.56636/200.</c>,
    /// not the double quotient of the same two numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A tripwire, and it guards against tidying rather than against a bug: written out,
    /// the constant looks like a needlessly awkward way to spell <c>FullSolidAngle/200</c>
    /// and invites exactly that simplification. Line 716 divides two default-real
    /// literals, so the original compares TU against a REAL*4 value; the double quotient
    /// sits 0.24 ulp below it.
    /// </para>
    /// <para>
    /// ⚠ The consequence is sharp, not vague, which is why this is worth pinning. Both
    /// readings round to the same float, so they can only ever disagree when TU lands
    /// exactly ON that float — where the original's <c>.gt.</c> is false and a
    /// double-threshold comparison is true. That is one specific decision, and this loop
    /// runs ~7e6 times on hp1050.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheNeighbourBudgetThresholdIsFoldedInSinglePrecision()
    {
        const double folded = 0.062831804156303406;
        const double asDouble = 12.56636f / 200.0;

        Assert.Equal(folded, StructureSimulation.TuExitThreshold);
        Assert.NotEqual(asDouble, StructureSimulation.TuExitThreshold);

        // The two differ, but only below the float grid they share - so the failure mode
        // this guards is invisible to any comparison made in single precision.
        Assert.Equal((float)asDouble, (float)StructureSimulation.TuExitThreshold);
        Assert.True(StructureSimulation.TuExitThreshold > asDouble);
    }

    [Theory]
    [MemberData(nameof(ArchivedRunFixture.ReplayableRuns), MemberType = typeof(ArchivedRunFixture))]
    [Trait("Category", "LongRunning")]
    public void TheDrawCountersMatchTheArchivedRunExactly(string id)
    {
        var replay = fixture.For(id);
        var run = replay.Run;
        var diagnostics = replay.Result.Diagnostics;
        output.WriteLine($"{run.Id}: {replay.Elapsed.TotalSeconds:F1} s");

        var counters = new (string Name, long Value)[]
        {
            ("nfx", diagnostics.BaseParticleDraws),
            ("nfy", diagnostics.SurroundingParticleDraws),
            ("nfq", diagnostics.PocketSizeDraws),
            ("nfw", diagnostics.BridgeDraws),
            ("nkarm", diagnostics.PocketsTotal),
            ("cycles_done", diagnostics.PassesDone),
            ("nbase_per_cycle", diagnostics.BaseParticlesPerCycle),
            ("nbase_accepted_cumulative", diagnostics.BaseParticlesAccepted),
        };

        var problems = new List<string>();
        foreach (var (name, value) in counters)
        {
            var expected = (long)run.Scalars[name];
            output.WriteLine($"{name,-26} {value,14} oracle {expected,14}");
            if (value != expected)
            {
                problems.Add($"{name}: {value}, oracle {expected}.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// L2, second group: the nine condition counters, with both of the original's
    /// divisors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Printed list-directed from a single, so every digit shown is real and the
    /// tolerance is the single's own. What makes this group worth its own test is the
    /// divisor: conditions 1-5 are divided by <c>FI+N</c> and conditions 6-9 by
    /// <c>FI</c>, because the first five are counted on the preparatory pass as well.
    /// Blending the two groups produces a number that means nothing, so the result
    /// keeps them apart and this checks both halves against the archive.
    /// </para>
    /// <para>
    /// ⚠ Condition 1 is zero here, and is zero in all 43 archived runs. That is the
    /// original's line-529 typo, which the port reproduces deliberately — a non-zero
    /// value would mean the typo had been "fixed".
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(ArchivedRunFixture.ReplayableRuns), MemberType = typeof(ArchivedRunFixture))]
    [Trait("Category", "LongRunning")]
    public void TheConditionCountersMatchTheArchivedRun(string id)
    {
        var replay = fixture.For(id);
        var conditions = replay.Result.Diagnostics.Conditions;
        var actual = new (string Name, double Value)[]
        {
            ("dok_above_dmax", conditions.DokAboveDmax),
            ("dok_below_dmin", conditions.DokBelowDmin),
            ("base_below_half_dok", conditions.BaseBelowHalfDok),
            ("base_above_two_dok", conditions.BaseAboveTwoDok),
            ("gap_above_4p7_base", conditions.GapAbove4p7Base),
            ("no_pockets", conditions.NoPockets),
            ("fewer_than_two_bridges", conditions.FewerThanTwoBridges),
            ("pocket_bridge_ratio_below_min", conditions.PocketBridgeRatioBelowMin),
            ("pocket_bridge_ratio_above_max", conditions.PocketBridgeRatioAboveMax),
        };

        var problems = new List<string>();
        foreach (var (name, value) in actual)
        {
            var expected = replay.Run.ConditionCounters[name];
            var error = expected == 0 ? Math.Abs(value) : Math.Abs((value - expected) / expected);
            output.WriteLine($"{name,-30} {value,-16:R} oracle {expected,-16:R} {error:E2}");
            if (error > 1e-6)
            {
                problems.Add($"{name}: {value:R}, oracle {expected:R}, {error:E2}.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// L2, third group: the derived scalars, on a run three hundred thousand times
    /// larger than the probe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This also tests a prediction. The probe's three misses are the three quantities
    /// reading a bridge volume, and the explanation is that one ulp of the math
    /// library, amplified by <c>TAN(GA)</c>, moves that volume by up to 6.1e-4 — see
    /// <c>ProbeRun.ThreeScalarsInheritTheMathLibrarysLastBit</c>. If that is right the
    /// per-bridge errors are independent and their sign is random, so the relative
    /// error of the total should fall roughly as one over the square root of the
    /// bridge count: 2-3.5e-6 at 92 bridges, and far smaller here.
    /// </para>
    /// <para>
    /// ⚠ A <b>larger</b> error here would falsify that explanation and point back at
    /// the formula. The tolerance is deliberately left at the probe's, so the run says
    /// which way it went instead of being fitted to pass.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(ArchivedRunFixture.ReplayableRuns), MemberType = typeof(ArchivedRunFixture))]
    [Trait("Category", "LongRunning")]
    public void TheDerivedScalarsMatchTheArchivedRun(string id)
    {
        var replay = fixture.For(id);
        var run = replay.Run;
        var result = replay.Result;

        var problems = new List<string>();
        foreach (var (name, expected) in run.Scalars.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (!TryRead(result, name, out var value))
            {
                problems.Add($"{name}: the result does not carry it.");
                continue;
            }

            // The tolerance is the print format's, chosen per quantity rather than per
            // group. Getting this wrong once already produced a false failure: dqmkm1
            // and dqmkm2 print F6.4, so the oracle holds four decimals, and asking them
            // for list-directed precision demands digits the report never showed.
            var (tolerance, relative) = Tolerance(name, expected);
            var error = relative ? Math.Abs((value - expected) / expected) : Math.Abs(value - expected);
            output.WriteLine(
                $"{name,-26} {value,-16:R} oracle {expected,-16:R} {(relative ? "rel" : "abs")} {error:E2}");

            // A quantity of the form |a - b| / b with a close to b has the arithmetic's
            // own floor under it, and the floor can be far above the format's tolerance.
            var allowed = relative ? tolerance * Math.Abs(expected) : tolerance;
            allowed = Math.Max(allowed, CancellationFloor(name));
            var absolute = Math.Abs(value - expected);
            if (absolute > allowed)
            {
                problems.Add($"{name}: {value:R}, oracle {expected:R}, {error:E2} over {tolerance:E0}.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// L2, fourth group: the thirteen printed arrays, cell by cell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The arrays come out of <c>Run</c> — the shipped result, not a reconstruction the
    /// test performs alongside it. That matters: a reconstruction would let the port
    /// hand a consumer one set of numbers while the test checked another, and the
    /// arrays are the part of the result most easily built two plausible ways. The
    /// independent truth is the archived <c>.m</c> either way.
    /// </para>
    /// <para>
    /// ⚠ Every normalising sum runs over the <b>whole allocation</b>, not over the
    /// printed length: the report shows the first <c>int(DPmax/Di)+2</c> cells of an
    /// array summed over all <c>Nkarm</c>.
    /// </para>
    /// <para>
    /// Tolerance is the format's: <c>arrayprint</c> writes <c>E9.3</c>, three
    /// significant digits, so half of the last one is 5e-3 relative.
    /// </para>
    /// <para>
    /// One class of cell is exempt from the relative comparison, and the exemption is a
    /// statement about the original, not a concession. Where the archived report holds a
    /// denormal — <c>pdoksmall(1)</c> prints 1.01E-38 — the original never assigned that
    /// cell; it is printing the allocation's contents. Running the original itself twice
    /// under wine gives a different value there, so no port can reproduce it, and the
    /// only demand that means anything is that our cell is negligible as well.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(ArchivedRunFixture.ReplayableRuns), MemberType = typeof(ArchivedRunFixture))]
    [Trait("Category", "LongRunning")]
    public void ThePrintedArraysMatchTheArchivedRun(string id)
    {
        var replay = fixture.For(id);
        var run = replay.Run;
        var result = replay.Result;
        var walls = result.PocketWallDistributions;

        var expected = new Dictionary<string, double[]>(StringComparer.Ordinal);
        foreach (var distribution in result.Distributions)
        {
            expected[distribution.Name] = [.. distribution.Values];
        }

        // The three length-valued arrays and the one diagnostic — the four the result
        // deliberately does not carry as distributions. ⚠ The report multiplies each
        // length by 1.00001e6 rather than 1e6, a nudge that keeps int() from truncating
        // a value sitting a hair under a whole micrometre; the nudge belongs to the
        // printing, so it is applied here and not stored in the result.
        expected["Dkarmcat"] = [.. walls.Categories.Select(AsPrinted)];
        expected["dokkarm43"] = [.. walls.MassMeanOxidiser.Select(value => AsPrinted(value.Value))];
        expected["dokkarm10"] = [.. walls.MeanOxidiser.Select(value => AsPrinted(value.Value))];
        expected["epsdokfr"] = [.. result.Diagnostics.FractionDrawAccuracy];

        static double AsPrinted(Length length) => (float)(length.Meters * 1.00001e6);

        var problems = new List<string>();
        foreach (var (name, mine) in expected)
        {
            if (!run.Arrays.TryGetValue(name, out var oracle))
            {
                problems.Add($"{name}: the reference does not record it.");
                continue;
            }

            if (mine.Length != oracle.Count)
            {
                problems.Add($"{name}: {mine.Length} cells, oracle {oracle.Count}.");
                continue;
            }

            // ⚠ epsdokfr carries the same cancellation floor as the epsilon scalars, and
            // for the same reason: each cell is |share - zx| / zx, where share is the
            // REAL*4 quotient of two counts. Where the sampler landed almost exactly on
            // the target the printed cell falls to a few times 1e-6, which is a handful
            // of ulps of share, and a flat relative tolerance then asks the last bit of
            // a single to be reproduced. Three of the six replayed runs fail on cell 0
            // alone without this — hp1 and r1 at 3.78e-6, r2 at 5.31e-6, all three
            // within 3.3e-8 absolute, well under the 1.19e-7 two-ulp floor. Every other
            // cell of every other array passes on the format tolerance by itself, so the
            // floor is zero for them and this changes nothing.
            var floor = name == "epsdokfr" ? Math.ScaleB(1.0, -23) : 0.0;

            var worst = 0.0;
            var worstAt = -1;
            for (var i = 0; i < mine.Length; i++)
            {
                // A cell the original left below the smallest normal single was never
                // written by it at all — the report is printing whatever the allocation
                // happened to hold. pdoksmall[0] is the known case: 1.01E-38 here, a
                // different denormal on a re-run of the original itself under wine. Such
                // a cell has no reference value, so the only meaningful demand is that
                // ours is negligible too.
                // Both an exact zero and a denormal land here: E9.3 prints 1.000E-45 for
                // a genuinely tiny number, so a printed zero means exactly zero, and the
                // demand in either case is the same — ours must be negligible too.
                var error = Math.Abs(oracle[i]) < Denormal
                    ? (Math.Abs(mine[i]) < Denormal ? 0.0 : 1.0)
                    : Math.Abs((mine[i] - oracle[i]) / oracle[i]);

                // The floor is applied per cell, not to the worst one afterwards: a
                // cell that clears the floor must not shield a later cell that does not.
                if (error > worst && Math.Abs(mine[i] - oracle[i]) > floor)
                {
                    (worst, worstAt) = (error, i);
                }
            }

            output.WriteLine($"{name,-14} {mine.Length,4} cells, worst {worst:E2} at [{worstAt}]");

            if (worst > 5e-3)
            {
                problems.Add(
                    $"{name}[{worstAt}]: {mine[worstAt]:R}, oracle {oracle[worstAt]:R}, rel {worst:E2}.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Reads one printed scalar out of the result, in whichever of the two records
    /// carries it, and always in the unit the report printed.
    /// </summary>
    private static bool TryRead(StructureResult result, string name, out double value)
    {
        if (result.PrintedLengths.TryGetValue(name, out var length))
        {
            value = length.Value.Micrometers;
            return true;
        }

        if (result.Printed.TryGetValue(name, out var scalar))
        {
            value = scalar.Value;
            return true;
        }

        value = double.NaN;
        return false;
    }

    /// <summary>
    /// The tolerance one printed scalar is owed, and whether it is relative.
    /// </summary>
    /// <remarks>
    /// Four classes, and each one is a fact about the <c>write</c> statement rather
    /// than a judgement about the quantity. Whole numbers print in full and are
    /// required to be exact; <c>F7.2</c> on micrometres leaves half of the last digit
    /// shown, so 0.005 µm absolute; <c>F6.4</c> leaves 5e-5 absolute; everything else
    /// is list-directed and shows every digit a single holds.
    /// </remarks>
    private static (double Tolerance, bool Relative) Tolerance(string name, double expected)
    {
        if (WholeNumbers.Contains(name))
        {
            return (0.0, false);
        }

        if (StructureResult.LengthValuedQuantities.Contains(name))
        {
            return (0.005, false);
        }

        if (name is "dqmkm1" or "dqmkm2")
        {
            return (5e-5, false);
        }

        // ⚠ The three quantities that read a bridge volume, and the only ones with a
        // loosened relative tolerance. The volume is ill-conditioned by line 1616 and
        // is not bit-reproducible across math libraries; see Geometry/BOOT.md. The
        // tolerance is the probe's, left unchanged on purpose so a larger run reports
        // where the error went instead of being fitted to pass.
        if (name is "zkarm" or "zkarm_cor1" or "zkarm_cor2")
        {
            return (1e-5, true);
        }

        // ⚠ fine_oxidiser_fraction is zero on most of the archive and there is no
        // relative error against zero — but it is NOT zero on all of it, and an earlier
        // version of this comment said it was. rps01 homogenises 0.1566 of the oxidiser
        // and rps02 0.7064, and on those two the flat 1e-9 absolute demanded nine
        // digits of a number the report prints list-directed to seven: rps02's oracle
        // reads 0.7064000, the port produces the float nearest to it, and the test
        // called that a failure at 3.7e-8. Where it is non-zero it is an ordinary
        // list-directed scalar; where it is zero the demand is that ours is zero too.
        if (name == "fine_oxidiser_fraction")
        {
            return expected == 0.0 ? (0.0, false) : (1e-6, true);
        }

        return (1e-6, true);
    }

    /// <summary>
    /// The absolute floor under a scalar the original computes as a relative
    /// difference of two nearly equal single-precision quantities. Zero for everything
    /// else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Six of the printed scalars have the form <c>|a - b| / b</c> with <c>a</c> and
    /// <c>b</c> agreeing to five or six digits — they exist to say how far the sample
    /// fell from the analytic value, so a <b>small</b> printed number is the normal
    /// case, and the smaller it is the more of it is the last bits of <c>a</c>. On
    /// <c>rps01</c>, <c>eps_all_dok</c> prints 8.65e-4; one ulp of the single-precision
    /// ratio it differences is 6e-8, so the fifth digit of that 8.65e-4 is already the
    /// last bit of the operand. Asking for six digits of it asks for digits the
    /// arithmetic never had.
    /// </para>
    /// <para>
    /// The floor is therefore <b>two ulps of a single</b>, 2^-23 = 1.19e-7, in the
    /// units of the printed epsilon itself — one ulp from each of the two operands
    /// being differenced. It is a count of ulps, not a fitted number: the measured
    /// worst case across the six replayed runs is 9.3e-8, which is 1.56 ulps, and the
    /// three scalars that reach it (<c>rps01</c>'s <c>eps_all_dok</c>,
    /// <c>eps_dok_base</c> and <c>eps_dok_surrounding</c>) sit within a few per cent of
    /// each other in ulps — the signature of a last-bit difference, not of a wrong
    /// formula.
    /// </para>
    /// <para>
    /// ⚠ <c>epsx1</c>…<c>epsx6</c> are deliberately <b>not</b> in this set although
    /// they are epsilons too. They measure the random streams, and every quantity they
    /// difference is REAL*8 in the original — there is no single-precision operand to
    /// put a floor under, and the archive agrees: they reproduce to 1e-9 relative.
    /// Giving them the floor would weaken a check that passes by three orders of
    /// magnitude.
    /// </para>
    /// </remarks>
    private static double CancellationFloor(string name) =>
        CancellingEpsilons.Contains(name) ? Math.ScaleB(1.0, -23) : 0.0;

    /// <summary>
    /// The epsilons built by differencing single-precision quantities: three over the
    /// oxidiser sizes (<c>EPSX3</c>, <c>EPSX1</c>, <c>EPSX2</c>, all against the
    /// analytic mean <c>DOKM</c>) and three over the pocket histogram, whose weights
    /// <c>QKS1</c> are REAL*4.
    /// </summary>
    private static IReadOnlySet<string> CancellingEpsilons { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "eps_all_dok", "eps_dok_base", "eps_dok_surrounding",
        "eps_pocket_distribution", "eps_pocket_moment3", "eps_pocket_moment4",
    };

    /// <summary>Printed scalars whose value is a count, and so must match exactly.</summary>
    private static IReadOnlySet<string> WholeNumbers { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "cycles_done", "nkarm", "nfx", "nfy", "nfq", "nfw",
        "nbase_per_cycle", "nbase_accepted_cumulative",
    };

    /// <summary>
    /// The printed scalars that legitimately reach the last line of
    /// <see cref="Tolerance"/> — list-directed, so every digit shown is real and the
    /// tolerance is the single's own.
    /// </summary>
    /// <remarks>
    /// Naming them is the whole point. Without this set the last line is a silent
    /// default, and the failure mode it hides is not a missing entry but a
    /// <b>misspelled</b> one: <c>Tolerance</c> matches by string, so a name typed
    /// <c>nfx_</c> in <see cref="WholeNumbers"/> falls through to 1e-6 relative, and a
    /// draw counter that must agree to the unit is checked to six digits instead — a
    /// test that still passes, still prints the counter, and no longer requires it to
    /// be right.
    /// </remarks>
    private static IReadOnlySet<string> ListDirectedScalars { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "epsx1", "epsx2", "epsx3", "epsx4", "epsx5", "epsx6",
        "gap_coefficient", "bridges_per_particle", "pocket_bridge_ratio", "jammed_fraction",
        "eps_all_dok", "eps_dok_base", "eps_dok_surrounding",
        "eps_pocket_distribution", "eps_pocket_moment3", "eps_pocket_moment4",
        "da_coef",
    };

    /// <summary>
    /// Every printed scalar is classified by exactly one branch of the tolerance table,
    /// and every name the table classifies is a scalar that exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a test of the checking apparatus rather than of the model, and it is
    /// here because the apparatus has the property that makes such a test worth
    /// writing: it fails <b>open</b>. A name no branch matches is not an error — it
    /// quietly receives the loosest tolerance in the table.
    /// </para>
    /// <para>
    /// Both directions are asserted. Unclassified names catch a scalar added to the
    /// result and never given a tolerance; classified names that do not exist catch the
    /// typo, which is the same defect seen from the side where it is visible. A probe
    /// supplies the names — the dictionaries are built by one code path for every run,
    /// so a 150 ms probe enumerates exactly what a ten-minute archived replay would.
    /// </para>
    /// <para>
    /// ⚠ <c>Tolerance</c> is not called here, and deliberately: calling it would only
    /// confirm that a function with a fallthrough returns something for every input.
    /// The claim is about the sets the branches read, so the sets are what is compared.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheToleranceTableClassifiesEveryPrintedScalarExactlyOnce()
    {
        var result = PropellantStructureModel.Run(ProbeRun.Configuration);
        var printed = result.Printed.Keys
            .Concat(result.PrintedLengths.Keys)
            .ToHashSet(StringComparer.Ordinal);

        var classified = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        Classify(nameof(WholeNumbers), WholeNumbers);
        Classify(nameof(StructureResult.LengthValuedQuantities), StructureResult.LengthValuedQuantities);
        Classify("F6.4", ["dqmkm1", "dqmkm2"]);
        Classify("bridge volume", ["zkarm", "zkarm_cor1", "zkarm_cor2"]);
        Classify("zero-or-relative", ["fine_oxidiser_fraction"]);
        Classify(nameof(ListDirectedScalars), ListDirectedScalars);

        var unclassified = printed.Where(name => !classified.ContainsKey(name)).Order().ToList();
        Assert.True(
            unclassified.Count == 0,
            $"printed but given no tolerance, so they silently take 1e-6 relative: "
                + string.Join(", ", unclassified));

        var phantom = classified.Keys.Where(name => !printed.Contains(name)).Order().ToList();
        Assert.True(
            phantom.Count == 0,
            $"named in the tolerance table but not printed - a typo here loosens the real "
                + $"name's tolerance instead of failing: {string.Join(", ", phantom)}");

        var twice = classified
            .Where(entry => entry.Value.Count > 1)
            .Select(entry => $"{entry.Key} ({string.Join(" and ", entry.Value)})")
            .Order()
            .ToList();
        Assert.True(
            twice.Count == 0,
            $"classified more than once, so which tolerance applies depends on branch order: "
                + string.Join(", ", twice));

        return;

        void Classify(string branch, IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                if (!classified.TryGetValue(name, out var branches))
                {
                    classified[name] = branches = [];
                }

                branches.Add(branch);
            }
        }
    }
}
