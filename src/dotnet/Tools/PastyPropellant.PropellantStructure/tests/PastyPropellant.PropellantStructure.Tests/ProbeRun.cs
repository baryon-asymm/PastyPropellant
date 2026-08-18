using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Geometry;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Results;
using PastyPropellant.PropellantStructure.Sampling;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// A twenty-particle run generated on demand under wine. Every integer the original
/// prints for it is reproduced exactly.
/// </summary>
/// <remarks>
/// <para>
/// This exists because the archived runs are too large to trace. The cheapest of them
/// draws ten million numbers, so a discrepancy there reports as «about one per cent»
/// and localises nothing. Twenty particles can be followed by hand.
/// </para>
/// <para>
/// The oracle is not an archived file: <c>PROBE.dat</c> was written for this test and
/// run under wine 9.0, which reproduces archived runs bit-for-bit (see
/// <c>reference/propstruct/README.md</c>). Its recipe is <c>hp2</c>'s as the report
/// prints it, at N = 20.
/// </para>
/// <para>
/// ⚠ The expectations are the original's own printed output, transcribed. Two of them
/// come from the console rather than the <c>.m</c> file, because the console prints
/// the condition counters raw while the file divides them: the file shows
/// <c>5) 4.95</c> and the console <c>198</c>, and 198 = 4.95 × 40. That arithmetic
/// confirms the first five counters are normalised on both passes — but <b>not</b> what
/// the divisor is, because this run has FI = N and so 40 is both <c>FI + N</c> and
/// <c>2N</c>. <see cref="ProbeRun3"/> is the fixture that separates them.
/// </para>
/// </remarks>
public sealed class ProbeRun
{
    /// <summary>The recipe of <c>PROBE.dat</c>, field for field.</summary>
    internal static StructureRunConfiguration Configuration => new()
    {
        OxidiserDensity = Density.FromKilogramsPerCubicMeter(1.95e3),
        BinderMetalDensity = Density.FromKilogramsPerCubicMeter(1.800e3),
        OxidiserMassFraction = 0.583,
        MetalMassFraction = 0.207,
        Fractions =
        [
            new OxidiserFraction(0.0103, Length.FromMicrometers(10), Length.FromMicrometers(50)),
            new OxidiserFraction(0.504, Length.FromMicrometers(113), Length.FromMicrometers(180)),
            new OxidiserFraction(0.486, Length.FromMicrometers(160), Length.FromMicrometers(315)),
        ],
        BaseParticles = 20,
        Cycles = 0,
        Law = SizeDistributionLaw.Surface,
        Generator = GeneratorSelection.Random2,
        ResolvedRunPath = null,
    };

    [Fact]
    public void EveryDrawCounterMatchesTheOracle()
    {
        var state = Simulate();

        Assert.Equal(915, state.Nfx);
        Assert.Equal(21788, state.Nfy);
        Assert.Equal(21788, state.Nfz);
        Assert.Equal(694, state.Nfq);
        Assert.Equal(3196, state.Nfw);
    }

    [Fact]
    public void EveryConditionCounterMatchesTheOracle()
    {
        var state = Simulate();

        // console output of the same run; the .m file's per-particle values are these
        // divided by FI+N for 1-5 and by FI for 6-9. Here FI = N = 20, so both divisors
        // read 40 and 20 respectively under either interpretation - see ProbeRun3.
        Assert.Equal(0, state.Conditions[1]);
        Assert.Equal(0, state.Conditions[2]);
        Assert.Equal(633, state.Conditions[3]);
        Assert.Equal(16780, state.Conditions[4]);
        Assert.Equal(198, state.Conditions[5]);
        Assert.Equal(0, state.Conditions[6]);
        Assert.Equal(0, state.Conditions[7]);
        Assert.Equal(44, state.Conditions[8]);
        Assert.Equal(0, state.Conditions[9]);
    }

    /// <summary>
    /// The two divisors are not a matter of interpretation: the same run printed both
    /// forms, and dividing one by the other gives 40 and 20 exactly.
    /// </summary>
    /// <remarks>
    /// ⚠ This run cannot say whether the first divisor is <c>FI + N</c> or <c>2N</c> —
    /// with FI = N they are the same 40. <see cref="ProbeRun3"/> runs the same recipe at
    /// KXX = 2, where FI = 40 and the two differ, and settles it in favour of
    /// <c>FI + N</c>.
    /// </remarks>
    [Fact]
    public void TheFirstFiveConditionsAreNormalisedOnBothPassesAndTheLastFourOnTheWorkingOne()
    {
        var state = Simulate();
        const int n = 20;

        // .m file, lines 47-49 and 52
        Assert.Equal(15.825, (double)state.Conditions[3] / (state.Fi + n), 6);
        Assert.Equal(419.5, (double)state.Conditions[4] / (state.Fi + n), 6);
        Assert.Equal(4.95, (double)state.Conditions[5] / (state.Fi + n), 6);
        Assert.Equal(2.2, (double)state.Conditions[8] / state.Fi, 6);

        Assert.Equal(n, state.Fi);
    }

    [Fact]
    public void TheTotalPocketCountMatchesTheOracle()
    {
        var state = Simulate();

        var pockets = 0L;
        foreach (var count in state.Qks)
        {
            pockets += count;
        }

        Assert.Equal(3263, pockets);
    }

    /// <summary>
    /// The literals above are the oracle's, and this is what stops them drifting from it.
    /// </summary>
    /// <remarks>
    /// The tree's rule is that the reference is files, not numbers in test code. The
    /// numbers are spelled out anyway, because two of them exist only on the console
    /// and because a reader should see what is being claimed — so the file is read here
    /// instead, and the claim is checked against it.
    /// </remarks>
    [Fact]
    public void TheExpectationsAreTheOnesTheOracleFilePrints()
    {
        var report = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe.m"));

        Assert.Contains("NFX =                   915 ;  NFY =                 21788 ;", report, StringComparison.Ordinal);
        Assert.Contains("NFQ =                   694 ;  NFW =                  3196 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nkarm =                  3263 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nbase =          20 +          20 ;", report, StringComparison.Ordinal);

        // per-particle forms of conditions 3, 4, 5 and 8; the raw counts asserted above
        // are these times 2N, 2N, 2N and N.
        Assert.Contains("3) Dbase < 0.5Dok  :   15.82500", report, StringComparison.Ordinal);
        Assert.Contains("4) Dbase > 2.0Dok  :   419.5000", report, StringComparison.Ordinal);
        Assert.Contains("5) l > 4.7Dbase    :   4.950000", report, StringComparison.Ordinal);
        Assert.Contains("8) Nkarm/Nmkm < min:   2.200000", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every derived scalar the oracle prints, against the tolerance of the format it
    /// was printed in.
    /// </summary>
    /// <remarks>
    /// The draw counters say the port walks the same path as the original; these say
    /// it does the same arithmetic at the end of it. They are separate failures: the
    /// summary reads accumulators, so a wrong summary leaves the counters untouched,
    /// and every one of these numbers is a line of the report a reader would quote.
    /// </remarks>
    [Fact]
    public void EveryDerivedScalarMatchesTheOracle()
    {
        var run = Run();
        var summary = Assert.IsType<PassSummary>(run.Summary);
        var state = run.State;
        var problems = new List<string>();

        // List-directed output: every digit the format can show.
        Relative("epsx(1)", summary.Eps[1], 5.3528011e-02);
        Relative("epsx(2)", summary.Eps[2], 5.2915573e-02);
        Relative("epsx(3)", summary.Eps[3], 1.6413927e-03);
        Relative("epsx(4)", summary.Eps[4], 1.5815496e-03);
        Relative("epsx(5)", summary.Eps[6], 1.8027067e-02);
        Relative("epsx(6)", summary.Eps[7], 1.8093467e-02);
        Relative("epsalldok", summary.Epsx3, 4.9046790e-03);
        Relative("epsdok(1)", summary.Epsx1, 2.6503550e-03);
        Relative("epsdok(2)", summary.Epsx2, 4.9871719e-03);
        Relative("epsfkarm", summary.Epsy, 0.1442056);
        Relative("epsmkarm(1)", summary.EpsMd4, 0.1169400);
        Relative("epsmkarm(2)", summary.EpsMd3, 8.4381789e-02);
        Relative("Medium coef", summary.Qmcoef, 2.450668);
        Relative("Medium number of bridges", (float)state.IbridgeTotal / state.Fi, 4.600000);
        Relative("Medium pocket/bridge ratio", state.NnTotal / state.Fi, 6.207262);
        Relative("Medium jammed fraction", state.JammedTotal / state.Fi, 6.5370291e-02);
        Relative("da_coef", run.DaCoefficient, 0.7389501);

        // ⚠ The only three scalars here that are NOT bit-exact, and the only three
        // that read a bridge volume. See ThreeScalarsInheritTheMathLibrarysLastBit.
        Amplified("Zkarm", 1f - summary.DolM1, 0.6058023);
        Amplified("Zkarm_cor(1)", 1f - summary.DolM2, 0.6058023);
        Amplified("Zkarm_cor(2)", 1f - summary.DolM3, 0.5930722);

        // F7.2 on micrometres: half of the last digit shown.
        Micrometres("Dok43a", run.DokM, 189.57);
        Micrometres("Dok43all", summary.Alldok43, 190.50);
        Micrometres("Dok43(1)", summary.Dok43b, 190.07);
        Micrometres("Dok43(2)", summary.Dok43s, 190.52);
        Micrometres("Dok43sd", (float)Math.Pow(run.DokSd, 0.5), 58.87);
        Micrometres("Dkarm43(1)", summary.Dp43, 148.56);
        Micrometres("Dkarm43(2)", summary.D432, 148.54);
        Micrometres("Dkarm43sd", summary.SdevP43, 47.63);
        Micrometres("Dkarm43_cor(1)", summary.Dkarm43Cor, 147.43);
        Micrometres("Dkarm43sd_cor(1)", summary.SdevP43Cor, 45.55);
        Micrometres("Dkarm43_cor(2)", summary.Dfmk432, 151.84);
        Micrometres("Dkarm43sd_cor(2)", summary.SdevP243, 39.07);
        Micrometres("Dkarm10", summary.Dqkarm, 84.87);
        Micrometres("Dkarm10_cor", summary.DqkarmCor, 84.57);

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        return;

        void Relative(string name, double actual, double expected)
        {
            if (Math.Abs(actual - expected) > Math.Abs(expected) * 1e-6)
            {
                problems.Add($"{name}: {actual:R}, oracle {expected:R}.");
            }
        }

        void Amplified(string name, double actual, double expected)
        {
            if (Math.Abs(actual - expected) > Math.Abs(expected) * 1e-5)
            {
                problems.Add($"{name}: {actual:R}, oracle {expected:R}.");
            }
        }

        void Micrometres(string name, double metres, double expected)
        {
            var actual = metres * 1e6;
            if (Math.Abs(actual - expected) > 0.005)
            {
                problems.Add($"{name}: {actual:F4} mkm, oracle {expected:F2}.");
            }
        }
    }

    /// <summary>
    /// Why three of the scalars above get a tolerance while the other thirty-one are
    /// required to be exact.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bridge volume is ill-conditioned, and the original made it so on purpose
    /// or by accident: line 1616 writes <c>BE = 3.14 - AL - DE</c> with 3.14 rather
    /// than π, so <c>GA = AL + BE/2</c> lands just under π/2 whenever the two radii
    /// are close — which inside one fraction is the common case. <c>TAN</c> there has
    /// a derivative of about 1.6e6, and the scan below measures the consequence: one
    /// single-precision ulp on an input moves the volume by up to 6.1e-4 relative.
    /// </para>
    /// <para>
    /// That is the whole explanation of the three misses: <c>ACOS</c>, <c>SIN</c> and
    /// <c>TAN</c> are the math library's, and no two libraries agree on the last bit
    /// of every REAL*4 result. The evidence is that the split is exactly along that
    /// line — <b>every</b> scalar that does not read a bridge volume matches the
    /// oracle bit for bit, and all three that do are off by 2-3.5e-6 in a sum of 92
    /// bridges. It is not a transcription error: an error in the formula would not
    /// respect that boundary.
    /// </para>
    /// <para>
    /// ⚠ So <c>Zkarm</c> is not reproducible to the eight digits the report prints
    /// it to, on any machine, by anyone. Its last three are noise.
    /// </para>
    /// </remarks>
    [Fact]
    public void ThreeScalarsInheritTheMathLibrarysLastBit()
    {
        // Half-diameters of the sizes this probe actually draws, against a pocket of
        // the size it actually reports.
        // ⚠ Not equal radii: there R1A = R2A and Q1M = Q2M, the two cone terms cancel
        // identically and the volume comes out negative. The interesting region is
        // near-equal, where GA is already against π/2 but nothing has cancelled.
        const float pocketRadius = 74.27e-6f;
        const float radius = 95e-6f;
        var worst = 0.0;

        for (var ratio = 0.60f; ratio < 0.999f; ratio += 0.0037f)
        {
            var other = radius * ratio;
            for (var gap = 1e-6f; gap < 40e-6f; gap += 1.3e-6f)
            {
                if (BridgeVolume.Between(radius, other, pocketRadius, gap, out var volume, out _)
                        != BridgeOutcome.Built
                    || volume == 0f)
                {
                    continue;
                }

                // One ulp on an input stands in for one ulp out of ACOS: both reach
                // TAN through GA, which is where the amplification lives.
                if (BridgeVolume.Between(
                        MathF.BitIncrement(radius), other, pocketRadius, gap, out var nudged, out _)
                    != BridgeOutcome.Built)
                {
                    continue;
                }

                worst = Math.Max(worst, Math.Abs((nudged - volume) / (double)volume));
            }
        }

        Assert.True(
            worst > 1e-5,
            $"One ulp moves the bridge volume by at most {worst:E2} relative. If that is now small, the "
            + "amplification through TAN(GA) is gone, and the three tolerances in "
            + $"{nameof(EveryDerivedScalarMatchesTheOracle)} have lost their justification — tighten them.");
    }

    /// <summary>
    /// The packed result carries every quantity the original's report prints, under the
    /// report's own name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A name check, not a value check — the values are L2's job, and on a twenty-particle
    /// probe most of them are meaningless anyway. What this catches is the failure mode
    /// packing actually has: a name misspelled or a quantity forgotten. That costs an hour
    /// to discover on an archived run and a second here, which is the whole point of
    /// keeping a probe.
    /// </para>
    /// <para>
    /// The list of names comes from the reference rather than from this file, so a
    /// quantity added to the report cannot be quietly left out of both.
    /// </para>
    /// </remarks>
    [Fact]
    public void ThePackedResultCarriesEveryPrintedQuantity()
    {
        var result = new StructureSimulation(Configuration, RandomStreamSet.Historical()).Run();
        var carried = new HashSet<string>(result.Printed.Keys, StringComparer.Ordinal);
        carried.UnionWith(result.PrintedLengths.Keys);

        var expected = ReferenceRuns.ById("hp1").Scalars.Keys;
        var missing = expected.Where(name => !carried.Contains(name)).ToArray();
        var extra = carried.Where(name => !expected.Contains(name)).ToArray();

        Assert.True(
            missing.Length == 0,
            $"the result does not carry: {string.Join(", ", missing)}.");
        Assert.True(
            extra.Length == 0,
            $"the result carries names the report does not print: {string.Join(", ", extra)}.");

        // The split between the two records is the unit, and getting it wrong is silent:
        // a length landing in Printed reads as a dimensionless number in metres.
        Assert.All(
            result.PrintedLengths.Keys,
            name => Assert.Contains(name, StructureResult.LengthValuedQuantities));
        Assert.All(
            result.Printed.Keys,
            name => Assert.DoesNotContain(name, StructureResult.LengthValuedQuantities));
    }

    /// <summary>Every printed array is there, with the classification the table gives it.</summary>
    [Fact]
    public void ThePackedResultCarriesEveryPrintedArray()
    {
        var result = new StructureSimulation(Configuration, RandomStreamSet.Historical()).Run();

        Assert.Equal(
            PrintedArrays.Distributions.Keys.Order(StringComparer.Ordinal),
            result.Distributions.Select(distribution => distribution.Name).Order(StringComparer.Ordinal));

        Assert.All(
            result.Distributions,
            distribution => Assert.Equal(
                PrintedArrays.Distributions[distribution.Name], distribution.Normalisation));

        // The four the result deliberately does not carry as distributions still have to
        // be reachable — three as lengths by pocket category, one as a diagnostic.
        var walls = result.PocketWallDistributions;
        Assert.Equal(walls.CategoryCount, walls.MassMeanOxidiser.Count);
        Assert.Equal(walls.CategoryCount, walls.MeanOxidiser.Count);
        Assert.Equal(walls.CategoryCount, walls.Rows.Count);
        Assert.NotEmpty(result.Diagnostics.FractionDrawAccuracy);
        Assert.Equal(Configuration.Fractions.Count, result.Diagnostics.FractionDrawAccuracy.Count);
    }

    private static RunState Simulate() => Run().State;

    private static StructureSimulation Run()
    {
        var simulation = new StructureSimulation(Configuration, RandomStreamSet.Historical());
        simulation.Simulate();
        return simulation;
    }
}
