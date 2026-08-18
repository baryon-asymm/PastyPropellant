using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// <see cref="ProbeRun"/>'s recipe under <c>JZ = 2</c> — the second size law, which no
/// archived run uses.
/// </summary>
/// <remarks>
/// <para>
/// The law decides both halves of one inversion: the formula turning a variate into a
/// diameter, and the weight turning a fraction's mass share into a number share. L1
/// already checks that those two agree with each other, which is the failure no fixture
/// can catch. This checks the other thing — that the pair the port implements is the
/// pair the original implements.
/// </para>
/// <para>
/// ⚠ It also documents a defect in the original: under this law <c>Dok43sd</c> comes out
/// <c>NaN</c>. See <see cref="TheAnalyticStandardDeviationIsNaNUnderThisLawAndHereIsWhy"/>.
/// </para>
/// </remarks>
public sealed class ProbeRun5
{
    /// <summary>The recipe of <c>PROBE5.dat</c>: <c>PROBE.dat</c> with JZZ 1 → 2.</summary>
    internal static StructureRunConfiguration Configuration =>
        ProbeRun.Configuration with { Law = SizeDistributionLaw.Volume };

    [Fact]
    public void EveryDrawCounterMatchesTheOracle()
    {
        var result = PropellantStructureModel.Run(Configuration);

        Assert.Equal(314, result.Printed["nfx"].Value);
        Assert.Equal(7255, result.Printed["nfy"].Value);
        Assert.Equal(570, result.Printed["nfq"].Value);
        Assert.Equal(1883, result.Printed["nfw"].Value);
        Assert.Equal(1821, result.Printed["nkarm"].Value);
    }

    [Fact]
    public void EveryConditionCounterMatchesTheOracle()
    {
        var state = Simulate();
        const int n = 20;

        // console output of the same run
        Assert.Equal(0, state.Conditions[1]);
        Assert.Equal(0, state.Conditions[2]);
        Assert.Equal(237, state.Conditions[3]);
        Assert.Equal(4389, state.Conditions[4]);
        Assert.Equal(3, state.Conditions[5]);
        Assert.Equal(0, state.Conditions[6]);
        Assert.Equal(0, state.Conditions[7]);
        Assert.Equal(34, state.Conditions[8]);
        Assert.Equal(0, state.Conditions[9]);

        // .m file, lines 46-48 and 51
        Assert.Equal(5.925, (double)state.Conditions[3] / (state.Fi + n), 6);
        Assert.Equal(109.725, (double)state.Conditions[4] / (state.Fi + n), 6);
        Assert.Equal(7.5000003e-02, (double)state.Conditions[5] / (state.Fi + n), 6);
        Assert.Equal(1.7, (double)state.Conditions[8] / state.Fi, 6);
    }

    [Fact]
    public void EveryDerivedScalarMatchesTheOracle()
    {
        var compare = new ProbeComparison(PropellantStructureModel.Run(Configuration));

        compare.Relative("epsx1", 1.5506804e-02);
        compare.Relative("epsx2", 1.4099002e-02);
        compare.Relative("epsx3", 2.8969049e-03);
        compare.Relative("epsx4", 2.6988387e-03);
        compare.Relative("epsx5", 1.0838389e-02);
        compare.Relative("epsx6", 5.9117079e-03);
        compare.Relative("eps_all_dok", 2.0625123e-03);
        compare.Relative("eps_dok_base", 4.4223506e-02);
        compare.Relative("eps_dok_surrounding", 2.6792000e-04);
        compare.Relative("eps_pocket_distribution", 0.1581909);
        compare.Relative("eps_pocket_moment4", 0.1299701);
        compare.Relative("eps_pocket_moment3", 9.0178318e-02);
        compare.Relative("gap_coefficient", 1.524604);
        compare.Relative("bridges_per_particle", 4.950000);
        compare.Relative("pocket_bridge_ratio", 5.821865);
        compare.Relative("jammed_fraction", 7.8997090e-02);
        compare.Relative("da_coef", 0.7389501);

        compare.Amplified("zkarm", 0.5524598);
        compare.Amplified("zkarm_cor1", 0.5524598);
        compare.Amplified("zkarm_cor2", 0.5841406);

        compare.Absolute("dqmkm1", 0.1393, 5e-5);
        compare.Absolute("dqmkm2", 0.5040, 5e-5);
        compare.Absolute("fine_oxidiser_fraction", 0.0, 0.0);

        // ⚠ 204.66, where every other probe on this recipe gives 189.57. The analytic
        // mean is law-dependent - it is the law's own third moment - so this number
        // moving is what says the law reached PARAM and not only SIZE.
        compare.Micrometres("dok43_analytic", 204.66);
        compare.Micrometres("dok43_all_v1", 205.09);
        compare.Micrometres("dok43_all_v2", 204.95);
        compare.Micrometres("dok43_base", 213.71);
        compare.Micrometres("dok43_surrounding", 204.72);
        compare.Micrometres("dok_max", 315.00);
        compare.Micrometres("dkarm43_v1", 168.80);
        compare.Micrometres("dkarm43_v2", 168.79);
        compare.Micrometres("dkarm43_sd", 51.54);
        compare.Micrometres("dkarm43_cor1", 169.36);
        compare.Micrometres("dkarm43_sd_cor1", 53.50);
        compare.Micrometres("dkarm43_cor2", 155.55);
        compare.Micrometres("dkarm43_sd_cor2", 40.83);
        compare.Micrometres("dkarm10", 114.94);
        compare.Micrometres("dkarm10_cor", 114.92);
        compare.Micrometres("dagg43_v1", 124.73);
        compare.Micrometres("dagg43_v2", 124.73);
        compare.Micrometres("dagg43_cor1", 125.15);
        compare.Micrometres("dagg43_cor2", 114.95);

        compare.NotANumber("dok43_sd");

        compare.Verify();
    }

    /// <summary>
    /// Under <c>JZ = 2</c> the original's analytic standard deviation of particle size is
    /// <c>NaN</c>, and the port reproduces it. This is why.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>DOKSD</c> is accumulated identically under both laws (lines 802 and 900): it is
    /// the mass-weighted mean square, <c>Σ Gdok·(D₁² + D₁D₂ + D₂²)/3</c>, which is the
    /// second moment of a distribution <b>uniform in D</b>. Then line 903 subtracts
    /// <c>DOKM²</c> — and under <c>JZ = 2</c> <c>DOKM</c> is not that distribution's mean
    /// but <c>0.8·DOK4/DOK3</c>, a ratio of fifth to fourth powers.
    /// </para>
    /// <para>
    /// On this recipe the accumulator comes to about 39 400 µm² while <c>DOKM²</c> is
    /// 204.66² ≈ 41 900, so the variance is negative and the report's <c>DOKSD**0.5</c>
    /// is <c>NaN</c>. It is not a near miss and not a rounding artefact: two different
    /// means are being subtracted from one second moment.
    /// </para>
    /// <para>
    /// ⚠ So <c>Dok43sd</c> carries no information under this law, in the original or in
    /// the port. Anything downstream that reads it must branch on the law. The port
    /// reproduces the <c>NaN</c> rather than repairing it, because repairing it would
    /// make the port disagree with the program it exists to reproduce — but nothing
    /// should consume it.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheAnalyticStandardDeviationIsNaNUnderThisLawAndHereIsWhy()
    {
        var law2 = PropellantStructureModel.Run(Configuration);
        var law1 = PropellantStructureModel.Run(ProbeRun.Configuration);

        Assert.True(double.IsNaN(law2.PrintedLengths["dok43_sd"].Value.Micrometers));

        // the same recipe under law 1 gives a real number, so the NaN is the law's and
        // not the recipe's
        var underLaw1 = law1.PrintedLengths["dok43_sd"].Value.Micrometers;
        Assert.False(double.IsNaN(underLaw1));
        Assert.Equal(58.87, underLaw1, 0.005);

        // and the arithmetic that produces it: the accumulator is law 1's second moment,
        // the mean subtracted from it is law 2's.
        var accumulator = underLaw1 * underLaw1
            + Math.Pow(law1.PrintedLengths["dok43_analytic"].Value.Micrometers, 2);
        var law2Mean = law2.PrintedLengths["dok43_analytic"].Value.Micrometers;
        Assert.True(
            accumulator < law2Mean * law2Mean,
            $"the variance is {accumulator - (law2Mean * law2Mean):F1} mkm^2 - if it is no longer negative, "
            + "the NaN has a different cause than the one documented here.");
    }

    /// <summary>The literals above are the oracle's.</summary>
    [Fact]
    public void TheExpectationsAreTheOnesTheOracleFilePrints()
    {
        var report = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe5.m"));

        Assert.Contains("% Nfr =  3; JZ = 2; Cycles =  1; N =     20; GSV = 2;", report, StringComparison.Ordinal);
        Assert.Contains("NFX =                   314 ;  NFY =                  7255 ;", report, StringComparison.Ordinal);
        Assert.Contains("NFQ =                   570 ;  NFW =                  1883 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nkarm =                  1821 ;", report, StringComparison.Ordinal);
        Assert.Contains("Dok43a =  204.66;", report, StringComparison.Ordinal);
        Assert.Contains("Dok43sd =  NaN", report, StringComparison.Ordinal);
    }

    private static RunState Simulate()
    {
        var simulation = new StructureSimulation(Configuration, RandomStreamSet.Historical());
        simulation.Simulate();
        return simulation.State;
    }
}
