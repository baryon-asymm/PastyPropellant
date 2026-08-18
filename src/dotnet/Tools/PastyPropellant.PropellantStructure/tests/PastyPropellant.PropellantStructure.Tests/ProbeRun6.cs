using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// <see cref="ProbeRun"/>'s recipe under <c>ivar = 1</c> — condition 3 discards only the
/// neighbour instead of restarting the whole base particle.
/// </summary>
/// <remarks>
/// <para>
/// All 43 archived runs use <c>ivar = 0</c>, so this branch had no reference of any kind
/// and the resolved record said so. It is the widest of the uncovered branches: the
/// original tests <c>ivar</c> for zero and treats every other value alike (line 549), and
/// what hangs on it is how much work a failed condition throws away.
/// </para>
/// <para>
/// ⚠ Its input is <b>not</b> a <c>.dat</c> file. <c>ivar</c> sits behind the "use default
/// parameters?" prompt, so the fixture is <c>PROBE.dat</c> plus a fixed sequence of
/// answers, recorded verbatim in <c>probe6.answers.txt</c> next to the report. The three
/// column-edit probes could each be reproduced from a file alone; this one cannot, and
/// the answers are part of the fixture.
/// </para>
/// </remarks>
public sealed class ProbeRun6
{
    /// <summary><c>PROBE.dat</c> with <c>ivar</c> answered as 1.</summary>
    internal static StructureRunConfiguration Configuration =>
        ProbeRun.Configuration with { CalculationVariant = 1 };

    [Fact]
    public void EveryDrawCounterMatchesTheOracle()
    {
        var result = PropellantStructureModel.Run(Configuration);

        Assert.Equal(386, result.Printed["nfx"].Value);
        Assert.Equal(14990, result.Printed["nfy"].Value);
        Assert.Equal(240, result.Printed["nfq"].Value);
        Assert.Equal(1923, result.Printed["nfw"].Value);
        Assert.Equal(2525, result.Printed["nkarm"].Value);
    }

    /// <summary>
    /// The counters are the branch: <c>ivar</c> decides what a broken condition 3 costs,
    /// and it costs fewer draws here than under <c>ivar = 0</c>.
    /// </summary>
    /// <remarks>
    /// Condition 3 fires <b>more</b> often (747 against 633) while the run draws far
    /// fewer numbers (386 base draws against 915). That is the branch stated in
    /// arithmetic: restarting the base particle throws away every draw already spent on
    /// it and re-fires the condition on the replacement, whereas discarding the
    /// neighbour keeps the base particle and retries only around it.
    /// </remarks>
    [Fact]
    public void EveryConditionCounterMatchesTheOracle()
    {
        var state = Simulate();
        const int n = 20;

        // console output of the same run
        Assert.Equal(0, state.Conditions[1]);
        Assert.Equal(0, state.Conditions[2]);
        Assert.Equal(747, state.Conditions[3]);
        Assert.Equal(11004, state.Conditions[4]);
        Assert.Equal(331, state.Conditions[5]);
        Assert.Equal(0, state.Conditions[6]);
        Assert.Equal(0, state.Conditions[7]);
        Assert.Equal(15, state.Conditions[8]);
        Assert.Equal(0, state.Conditions[9]);

        // .m file, lines 46-48 and 51
        Assert.Equal(18.675, (double)state.Conditions[3] / (state.Fi + n), 6);
        Assert.Equal(275.1, (double)state.Conditions[4] / (state.Fi + n), 6);
        Assert.Equal(8.275, (double)state.Conditions[5] / (state.Fi + n), 6);
        Assert.Equal(0.75, (double)state.Conditions[8] / state.Fi, 6);
    }

    [Fact]
    public void EveryDerivedScalarMatchesTheOracle()
    {
        var compare = new ProbeComparison(PropellantStructureModel.Run(Configuration));

        compare.Relative("epsx1", 3.3578634e-02);
        compare.Relative("epsx2", 3.2160223e-02);
        compare.Relative("epsx3", 3.2293797e-04);
        compare.Relative("epsx4", 3.2871962e-04);
        compare.Relative("epsx5", 3.2313943e-02);
        compare.Relative("epsx6", 1.1133790e-02);
        compare.Relative("eps_all_dok", 8.8572698e-03);
        compare.Relative("eps_dok_base", 7.8237385e-02);
        compare.Relative("eps_dok_surrounding", 7.1008303e-03);
        compare.Relative("eps_pocket_distribution", 0.1544323);
        compare.Relative("eps_pocket_moment4", 0.1259665);
        compare.Relative("eps_pocket_moment3", 8.9340769e-02);
        compare.Relative("gap_coefficient", 3.169549);
        compare.Relative("bridges_per_particle", 4.750000);
        compare.Relative("pocket_bridge_ratio", 5.851666);
        compare.Relative("jammed_fraction", 6.9066785e-02);
        compare.Relative("da_coef", 0.7389501);

        compare.Amplified("zkarm", 0.5244671);
        compare.Amplified("zkarm_cor1", 0.5244671);
        compare.Amplified("zkarm_cor2", 0.6220915);

        compare.Absolute("dqmkm1", 0.1482, 5e-5);
        compare.Absolute("dqmkm2", 0.5102, 5e-5);
        compare.Absolute("fine_oxidiser_fraction", 0.0, 0.0);

        compare.Micrometres("dok43_analytic", 189.57);
        compare.Micrometres("dok43_sd", 58.87);
        compare.Micrometres("dok43_all_v1", 191.25);
        compare.Micrometres("dok43_all_v2", 191.30);
        compare.Micrometres("dok43_base", 204.40);
        compare.Micrometres("dok43_surrounding", 190.92);
        compare.Micrometres("dok_max", 315.00);
        compare.Micrometres("dkarm43_v1", 159.73);
        compare.Micrometres("dkarm43_v2", 159.72);
        compare.Micrometres("dkarm43_sd", 49.83);
        compare.Micrometres("dkarm43_cor1", 152.43);
        compare.Micrometres("dkarm43_sd_cor1", 42.71);
        compare.Micrometres("dkarm43_cor2", 152.26);
        compare.Micrometres("dkarm43_sd_cor2", 34.77);
        compare.Micrometres("dkarm10", 95.91);
        compare.Micrometres("dkarm10_cor", 94.83);
        compare.Micrometres("dagg43_v1", 118.03);
        compare.Micrometres("dagg43_v2", 118.03);
        compare.Micrometres("dagg43_cor1", 112.64);
        compare.Micrometres("dagg43_cor2", 112.51);

        compare.Verify();
    }

    /// <summary>
    /// The original tests <c>ivar</c> for zero and nothing else, so every non-zero value
    /// must give the same run.
    /// </summary>
    /// <remarks>
    /// Line 549 is <c>if (ivar.eq.0)</c>. A port that compared against 1, or switched on
    /// the value, would pass every test above and diverge on the first configuration
    /// carrying 2 — which is a value the validator deliberately accepts, because there is
    /// no invalid <c>ivar</c>, only an unverified one.
    /// </remarks>
    [Fact]
    public void EveryNonZeroCalculationVariantIsTheSameBranch()
    {
        var one = PropellantStructureModel.Run(Configuration);
        var seven = PropellantStructureModel.Run(Configuration with { CalculationVariant = 7 });

        Assert.Equal(one.Printed["nfx"].Value, seven.Printed["nfx"].Value);
        Assert.Equal(one.Printed["nkarm"].Value, seven.Printed["nkarm"].Value);
        Assert.Equal(one.Printed["zkarm"].Value, seven.Printed["zkarm"].Value);

        // and it is a different run from ivar = 0, or the branch is not reached at all
        var zero = PropellantStructureModel.Run(ProbeRun.Configuration);
        Assert.NotEqual(one.Printed["nfx"].Value, zero.Printed["nfx"].Value);
    }

    /// <summary>The literals above are the oracle's.</summary>
    [Fact]
    public void TheExpectationsAreTheOnesTheOracleFilePrints()
    {
        var report = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe6.m"));

        // ⚠ The header names PROBE.dat, the same file ProbeRun uses. What separates the
        // two runs is the answer sequence, not the input file - so the variant line is
        // the only thing in the report that says which run this is.
        Assert.Contains("% Input filename: PROBE.dat", report, StringComparison.Ordinal);
        Assert.Contains("% Calculation variant =           1 ;", report, StringComparison.Ordinal);

        Assert.Contains("NFX =                   386 ;  NFY =                 14990 ;", report, StringComparison.Ordinal);
        Assert.Contains("NFQ =                   240 ;  NFW =                  1923 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nkarm =                  2525 ;", report, StringComparison.Ordinal);
        Assert.Contains("Zkarm =   0.5244671", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// The answer sequence that produced the report is kept next to it, because the
    /// report alone does not determine its own input.
    /// </summary>
    /// <remarks>
    /// Every other probe can be regenerated from a <c>.dat</c> file. This one needs the
    /// stdin as well, and a fixture that cannot be regenerated is a number nobody can
    /// re-derive.
    /// </remarks>
    [Fact]
    public void TheAnswerSequenceIsKeptWithTheReport()
    {
        var answers = File.ReadAllLines(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe6.answers.txt"));

        Assert.Equal(["PROBE", "n", "13", "1", "0"], answers);
    }

    private static RunState Simulate()
    {
        var simulation = new StructureSimulation(Configuration, RandomStreamSet.Historical());
        simulation.Simulate();
        return simulation.State;
    }
}
