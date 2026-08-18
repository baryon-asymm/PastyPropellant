using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Results;
using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// The same twenty-particle recipe as <see cref="ProbeRun"/>, run with <c>KXX = 2</c> —
/// the multi-pass branch, which no archived run reaches.
/// </summary>
/// <remarks>
/// <para>
/// All 43 archived runs carry <c>Cycles = 1</c>, which the original normalises to a
/// single working pass. So every scalar the archive can adjudicate was produced by one
/// pass, and the accumulate-across-passes code — which is most of the summary — was
/// reproduced against nothing. <c>PROBE3.dat</c> is <c>PROBE.dat</c> with one column
/// changed, run under the same wine 9.0 that reproduces the archive bit-for-bit, and it
/// is the first oracle that branch has.
/// </para>
/// <para>
/// ⚠ It settles a question the archive is structurally unable to settle. Conditions 1-5
/// are divided by <c>FI + N</c> and 6-9 by <c>FI</c>; on a single-pass run <c>FI = N</c>,
/// so <c>FI + N</c> and <c>2N</c> are the same number and the two readings cannot be
/// told apart. Here <c>FI = 40</c> and <c>N = 20</c>, so they differ — and the printed
/// 15.85 is 951/60, not 951/40. See
/// <see cref="TheConditionDivisorsAreFiPlusNAndFiNotTwiceN"/>.
/// </para>
/// </remarks>
public sealed class ProbeRun3
{
    /// <summary>
    /// <see cref="ProbeRun.Configuration"/> with two working passes instead of one.
    /// </summary>
    /// <remarks>
    /// Nothing else differs, and that is the point: the two probes share a recipe, so
    /// every difference between their reports is attributable to the pass count alone.
    /// </remarks>
    internal static StructureRunConfiguration Configuration =>
        ProbeRun.Configuration with { Cycles = 2 };

    /// <summary>The pass structure: one preparatory pass, then <c>KXX</c> working ones.</summary>
    /// <remarks>
    /// <c>Nbase = 20 + 40</c> is the whole claim. The left number is N, drawn afresh each
    /// pass; the right is FI, which counts accepted base particles over the working
    /// passes and therefore comes to 2N here and to N in the single-pass probe.
    /// </remarks>
    [Fact]
    public void TwoWorkingPassesAcceptTwiceTheBaseParticles()
    {
        var result = PropellantStructureModel.Run(Configuration);

        Assert.Equal(2, result.Printed["cycles_done"].Value);
        Assert.Equal(20, result.Printed["nbase_per_cycle"].Value);
        Assert.Equal(40, result.Printed["nbase_accepted_cumulative"].Value);
    }

    [Fact]
    public void EveryDrawCounterMatchesTheOracle()
    {
        var result = PropellantStructureModel.Run(Configuration);

        Assert.Equal(1372, result.Printed["nfx"].Value);
        Assert.Equal(33270, result.Printed["nfy"].Value);
        Assert.Equal(1219, result.Printed["nfq"].Value);
        Assert.Equal(5453, result.Printed["nfw"].Value);
        Assert.Equal(5001, result.Printed["nkarm"].Value);
    }

    /// <summary>
    /// The two condition divisors, on the only run in the tree that can distinguish them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The raw counters come from the console, the per-particle values from the <c>.m</c>
    /// file, and the arithmetic between them is the evidence:
    /// </para>
    /// <list type="bullet">
    /// <item>951 / 60 = 15.85 and 951 / 40 = 23.775 — the report prints 15.85, so the
    /// divisor is <c>FI + N</c>.</item>
    /// <item>74 / 40 = 1.85 and 74 / 60 = 1.2333 — the report prints 1.85, so condition 8
    /// is divided by <c>FI</c> alone.</item>
    /// </list>
    /// <para>
    /// ⚠ On <see cref="ProbeRun"/> both divisors evaluate to 40 and every one of these
    /// checks passes under either reading. That is why this fixture exists: a port can be
    /// wrong about the divisor and green against the entire archive.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheConditionDivisorsAreFiPlusNAndFiNotTwiceN()
    {
        var simulation = Simulate();
        const int n = 20;

        Assert.Equal(40, simulation.Fi);

        // the premise of the whole fixture: on this run the two candidate divisors are
        // different numbers, which is exactly what no archived run offers.
        Assert.True(
            simulation.Fi + n != 2 * n,
            "FI + N and 2N coincide here, so this fixture no longer distinguishes them.");

        // console output of the same run
        Assert.Equal(951, simulation.Conditions[3]);
        Assert.Equal(25598, simulation.Conditions[4]);
        Assert.Equal(287, simulation.Conditions[5]);
        Assert.Equal(74, simulation.Conditions[8]);

        // .m file, lines 46-48 and 51
        Assert.Equal(15.85, (double)simulation.Conditions[3] / (simulation.Fi + n), 6);
        Assert.Equal(426.6333, (double)simulation.Conditions[4] / (simulation.Fi + n), 4);
        Assert.Equal(4.783333, (double)simulation.Conditions[5] / (simulation.Fi + n), 6);
        Assert.Equal(1.850000, (double)simulation.Conditions[8] / simulation.Fi, 6);

        // the conditions the run never breaks, on both divisors
        foreach (var index in (int[])[1, 2, 6, 7, 9])
        {
            Assert.Equal(0, simulation.Conditions[index]);
        }
    }

    /// <summary>
    /// Every derived scalar of the multi-pass run, against the tolerance of the format
    /// it was printed in.
    /// </summary>
    /// <remarks>
    /// Read off the shipped result rather than the accumulators, so this covers the
    /// packing as well as the arithmetic. The tolerances are <see cref="ProbeRun"/>'s and
    /// for the same reasons — including the three <c>Zkarm</c> scalars, which read a
    /// bridge volume and so inherit the math library's last bit.
    /// </remarks>
    [Fact]
    public void EveryDerivedScalarMatchesTheOracle()
    {
        var compare = new ProbeComparison(PropellantStructureModel.Run(Configuration));

        compare.Relative("epsx1", 3.5132408e-02);
        compare.Relative("epsx2", 3.4669697e-02);
        compare.Relative("epsx3", 2.3698807e-04);
        compare.Relative("epsx4", 1.9681454e-04);
        compare.Relative("epsx5", 1.9383430e-04);
        compare.Relative("epsx6", 9.3931556e-03);
        compare.Relative("eps_all_dok", 4.6518227e-04);
        compare.Relative("eps_dok_base", 9.1434876e-03);
        compare.Relative("eps_dok_surrounding", 8.2071975e-04);
        compare.Relative("eps_pocket_distribution", 0.1112641);
        compare.Relative("eps_pocket_moment4", 8.9897901e-02);
        compare.Relative("eps_pocket_moment3", 6.5559559e-02);
        compare.Relative("gap_coefficient", 2.417842);
        compare.Relative("bridges_per_particle", 5.075000);
        compare.Relative("pocket_bridge_ratio", 5.647142);
        compare.Relative("jammed_fraction", 6.6454396e-02);
        compare.Relative("da_coef", 0.7389501);

        // ⚠ The three that read a bridge volume; see
        // ProbeRun.ThreeScalarsInheritTheMathLibrarysLastBit.
        compare.Amplified("zkarm", 0.5869849);
        compare.Amplified("zkarm_cor1", 0.5869849);
        compare.Amplified("zkarm_cor2", 0.5727881);

        compare.Absolute("dqmkm1", 0.1448, 5e-5);
        compare.Absolute("dqmkm2", 0.4968, 5e-5);

        // The run homogenises no oxidiser at all, so this one is exact rather than
        // merely close.
        compare.Absolute("fine_oxidiser_fraction", 0.0, 0.0);

        compare.Micrometres("dok43_analytic", 189.57);
        compare.Micrometres("dok43_all_v1", 189.66);
        compare.Micrometres("dok43_all_v2", 189.69);
        compare.Micrometres("dok43_base", 187.84);
        compare.Micrometres("dok43_surrounding", 189.73);
        compare.Micrometres("dok_max", 315.00);
        compare.Micrometres("dok43_sd", 58.87);
        compare.Micrometres("dkarm43_v1", 146.75);
        compare.Micrometres("dkarm43_v2", 146.73);
        compare.Micrometres("dkarm43_sd", 45.75);
        compare.Micrometres("dkarm43_cor1", 145.74);
        compare.Micrometres("dkarm43_sd_cor1", 44.09);
        compare.Micrometres("dkarm43_cor2", 149.83);
        compare.Micrometres("dkarm43_sd_cor2", 37.89);
        compare.Micrometres("dkarm10", 85.59);
        compare.Micrometres("dkarm10_cor", 85.55);
        compare.Micrometres("dagg43_v1", 108.44);
        compare.Micrometres("dagg43_v2", 108.42);
        compare.Micrometres("dagg43_cor1", 107.70);
        compare.Micrometres("dagg43_cor2", 110.72);

        compare.Verify();
    }

    /// <summary>The oxidiser fraction accuracies, which the report prints as an array.</summary>
    [Fact]
    public void TheFractionDrawAccuraciesMatchTheOracle()
    {
        var accuracy = PropellantStructureModel.Run(Configuration).Diagnostics.FractionDrawAccuracy;

        // epsdokfr, printed E9.3 - three digits of mantissa and no more.
        Assert.Equal(3, accuracy.Count);
        Assert.Equal(0.143e-02, accuracy[0], 0.143e-02 * 5e-3);
        Assert.Equal(0.135e-01, accuracy[1], 0.135e-01 * 5e-3);
        Assert.Equal(0.141e-01, accuracy[2], 0.141e-01 * 5e-3);
    }

    /// <summary>
    /// The first working pass of a two-pass run is the one-pass run, to every digit the
    /// report prints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At <c>KXX &gt; 1</c> the original also prints per-pass arrays — <c>epsfkarm_n</c>,
    /// <c>epszkarm_n</c> and four more — and their first entries are 0.144206 and
    /// 0.605802, which are <see cref="ProbeRun"/>'s <c>epsfkarm</c> = 0.1442056 and
    /// <c>Zkarm</c> = 0.6058023 to the digits <c>E9.3</c> preserves.
    /// </para>
    /// <para>
    /// That is worth an assertion of its own because it pins down what a pass is. The
    /// accumulators are zeroed once, before all passes, so the second pass continues the
    /// first rather than restarting it; if instead the preparatory pass differed by pass
    /// count, or a per-pass reset were missing where one is needed, pass 1 of this run
    /// would not equal the whole of the other. The comparison is between two independent
    /// wine runs of the original, so it also confirms the fixture itself.
    /// </para>
    /// <para>
    /// The arrays themselves are checked in <see cref="TheSixPerPassArraysMatchTheOracle"/>;
    /// this test is about the identity between a pass and a run, which holds whether or
    /// not the port keeps a history.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheFirstPassOfTheTwoPassRunIsTheOnePassRun()
    {
        var report = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe3.m"));

        Assert.Contains("epsfkarm_n = [0.144206E+00 0.111264E+00  ];", report, StringComparison.Ordinal);
        Assert.Contains("epszkarm_n = [0.605802E+00 0.586985E+00  ];", report, StringComparison.Ordinal);

        // The same two quantities as the single-pass probe reports them. ⚠ The two
        // tolerances differ and must: eps_pocket_distribution is held to what E9.3 keeps
        // (5e-7 absolute), while zkarm reads a bridge volume and so carries the
        // math-library deviation documented in
        // ProbeRun.ThreeScalarsInheritTheMathLibrarysLastBit - 2.3e-6 relative here,
        // inside the 1e-5 that scalar is allowed everywhere else in the suite. Holding
        // it to E9.3 would be asserting a precision the quantity does not have on any
        // machine.
        var single = PropellantStructureModel.Run(ProbeRun.Configuration);
        Assert.Equal(0.144206, single.Printed["eps_pocket_distribution"].Value, 5e-7);
        Assert.Equal(0.605802, single.Printed["zkarm"].Value, 0.605802 * 1e-5);

        // and the second entry of each is where the two-pass run ends up
        var two = PropellantStructureModel.Run(Configuration);
        Assert.Equal(0.111264, two.Printed["eps_pocket_distribution"].Value, 5e-7);
        Assert.Equal(0.586985, two.Printed["zkarm"].Value, 0.586985 * 1e-5);
    }

    /// <summary>
    /// The six <c>*_n</c> arrays, both passes, against the oracle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the last of the original's fourteen printed arrays to be reproduced,
    /// and the only ones the archive cannot adjudicate: they are printed under
    /// <c>KXX &gt; 1</c> and no archived run makes more than one pass. <c>probe3.m</c>
    /// is the fixture that unblocks them, which is why <c>Results/API.md</c> made the
    /// port of this block conditional on it rather than on anybody wanting the numbers.
    /// </para>
    /// <para>
    /// The tolerance is <c>arrayprint2</c>'s. <c>E12.6</c> carries a six-digit mantissa
    /// in <c>[0.1, 1)</c>, so half an ulp of the print is at worst
    /// <c>5e-7 / 0.1 = 5e-6</c> relative, and no entry can be held tighter than that
    /// however exact the port is.
    /// </para>
    /// <para>
    /// ⚠ Two of the six differ from the printed scalar of the same name — see
    /// <see cref="PassConvergence"/>. <c>epsdok43_n</c> is signed where
    /// <c>eps_all_dok</c> is absolute (both are positive on this run, so this fixture
    /// pins the magnitude and not the sign), and <c>epsdoksd_n</c> is over variances —
    /// which is also why it is the one entry the print tolerance cannot be applied to;
    /// see <see cref="VarianceErrorFloor"/>.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheSixPerPassArraysMatchTheOracle()
    {
        var convergence = PropellantStructureModel.Run(Configuration).Diagnostics.Convergence;

        Assert.Equal(2, convergence.Count);

        var oracle = new PassConvergence[]
        {
            new(0.144206E+00, 0.843818E-01, 0.116940E+00, 0.490468E-02, 0.306973E-01, 0.605802E+00),
            new(0.111264E+00, 0.655596E-01, 0.898979E-01, 0.465182E-03, 0.235990E-01, 0.586985E+00),
        };

        var problems = new List<string>();
        for (var pass = 0; pass < oracle.Length; pass++)
        {
            Compare("epsfkarm_n", convergence[pass].PocketDistributionError, oracle[pass].PocketDistributionError, 0.0);
            Compare("epsm3karm_n", convergence[pass].PocketMoment3Error, oracle[pass].PocketMoment3Error, 0.0);
            Compare("epsm4karm_n", convergence[pass].PocketMoment4Error, oracle[pass].PocketMoment4Error, 0.0);
            Compare("epsdok43_n", convergence[pass].OxidiserMeanError, oracle[pass].OxidiserMeanError, 0.0);
            Compare("epsdoksd_n", convergence[pass].OxidiserVarianceError, oracle[pass].OxidiserVarianceError, VarianceErrorFloor);
            Compare("epszkarm_n", convergence[pass].PocketMassFraction, oracle[pass].PocketMassFraction, 0.0);

            void Compare(string name, double mine, double expected, double floor)
            {
                var allowed = Math.Max(5e-6 * Math.Abs(expected), floor);
                if (Math.Abs(mine - expected) > allowed)
                {
                    var error = Math.Abs((mine - expected) / expected);
                    problems.Add(
                        $"{name}({pass + 1}): {mine:R}, oracle {expected:R}, {error:E2} rel, "
                        + $"{Math.Abs(mine - expected):E2} abs over {allowed:E2}.");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// The absolute resolution of <c>epsdoksd_n</c> — one last bit of <c>ALLDOK243</c>,
    /// which is far above what its six printed digits suggest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>epsdoksd_n</c> is <c>(ALLDOKsd - DokSD) / DokSD</c>, and it cancels
    /// <b>twice</b>. Once at the top, where the two variances agree to three per cent;
    /// and once inside <c>ALLDOKsd</c> itself, which line 812 builds as
    /// <c>ALLDOK243 - ALLDOK432**2</c> from two second moments that agree to a factor
    /// of eleven. So a single last bit of the <c>REAL*4</c> <c>ALLDOK243</c> — a
    /// quantity of order <c>3.95e-8</c>, whose ulp is <c>4.7e-15</c> — arrives at the
    /// printed number divided by <c>DokSD ≈ 3.47e-9</c>, as <b>1.36e-6</b> absolute.
    /// Against a value of 0.024 that is 5.8e-5 relative, an order of magnitude wider
    /// than the 5e-6 the format implies.
    /// </para>
    /// <para>
    /// The port lands 0.58 and 0.93 of that ulp away on the two passes, so both entries
    /// are inside one bit of the original and neither is evidence of anything else. It
    /// is the same account as the L2 epsilons, one cancellation deeper — and it is a
    /// statement about the quantity, not about the port: no implementation in single
    /// precision can pin <c>epsdoksd_n</c> tighter, on any machine.
    /// </para>
    /// <para>
    /// ⚠ The floor is derived here from the oracle's own printed lengths rather than
    /// written down as a number, so it cannot quietly be widened to fit a future
    /// failure: <c>DokSD = Dok43sd²</c> and <c>ALLDOK243 = Dok43_2² + ALLDOKsd</c>, and
    /// both lengths are printed in the same report.
    /// </para>
    /// </remarks>
    private static double VarianceErrorFloor
    {
        get
        {
            const double dokSd = 58.87e-6 * 58.87e-6;              // Dok43sd, printed
            const double allDok432 = 189.69e-6 * 189.69e-6;        // Dok43_2, printed
            var allDokSd = dokSd * (1.0 + 0.0306973);              // epsdoksd_n itself
            return Math.ScaleB(1.0, -23) * (allDok432 + allDokSd) / dokSd;
        }
    }

    /// <summary>
    /// A single-pass run keeps no history, because the original prints none.
    /// </summary>
    /// <remarks>
    /// Empty rather than one entry long: the guard in the original is on <c>KXX</c>, not
    /// on the pass having happened, so a one-entry history would be the port inventing a
    /// number no report contains.
    /// </remarks>
    [Fact]
    public void TheOnePassRunHasNoPerPassHistory()
    {
        var single = PropellantStructureModel.Run(ProbeRun.Configuration);

        Assert.Equal(1, single.Diagnostics.PassesDone);
        Assert.Empty(single.Diagnostics.Convergence);
    }

    /// <summary>
    /// The literals above are the oracle's, and this is what stops them drifting from it.
    /// </summary>
    [Fact]
    public void TheExpectationsAreTheOnesTheOracleFilePrints()
    {
        var report = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe3.m"));

        // the input the fixture claims to be
        Assert.Contains("Nfr =  3; JZ = 1; Cycles =  2; N =     20; GSV = 2;", report, StringComparison.Ordinal);

        Assert.Contains("NFX =                  1372 ;  NFY =                 33270 ;", report, StringComparison.Ordinal);
        Assert.Contains("NFQ =                  1219 ;  NFW =                  5453 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nkarm =                  5001 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nbase =          20 +          40 ;", report, StringComparison.Ordinal);

        // per-particle forms of conditions 3, 4, 5 and 8; the raw counts asserted above
        // are these times FI+N, FI+N, FI+N and FI.
        Assert.Contains("3) Dbase < 0.5Dok  :   15.85000", report, StringComparison.Ordinal);
        Assert.Contains("4) Dbase > 2.0Dok  :   426.6333", report, StringComparison.Ordinal);
        Assert.Contains("5) l > 4.7Dbase    :   4.783333", report, StringComparison.Ordinal);
        Assert.Contains("8) Nkarm/Nmkm < min:   1.850000", report, StringComparison.Ordinal);

        Assert.Contains("Zkarm =   0.5869849", report, StringComparison.Ordinal);
        Assert.Contains("epsdokfr = [0.143E-02 0.135E-01 0.141E-01  ];", report, StringComparison.Ordinal);

        // the six per-pass arrays, exactly as printed
        Assert.Contains("epsm3karm_n = [0.843818E-01 0.655596E-01  ];", report, StringComparison.Ordinal);
        Assert.Contains("epsm4karm_n = [0.116940E+00 0.898979E-01  ];", report, StringComparison.Ordinal);
        Assert.Contains("epsdok43_n = [0.490468E-02 0.465182E-03  ];", report, StringComparison.Ordinal);
        Assert.Contains("epsdoksd_n = [0.306973E-01 0.235990E-01  ];", report, StringComparison.Ordinal);
    }

    private static RunState Simulate()
    {
        var simulation = new StructureSimulation(Configuration, RandomStreamSet.Historical());
        simulation.Simulate();
        return simulation.State;
    }
}
