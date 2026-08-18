using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// <see cref="ProbeRun"/>'s recipe driven by <c>gsv = 1</c> — the toy generator, which
/// no archived run uses and which the port did not implement until this fixture existed.
/// </summary>
/// <remarks>
/// <para>
/// The branch mattered more than its obscurity suggests. Before this, the kernel built
/// <c>RandomStreamSet.Historical()</c> unconditionally: a configuration asking for
/// <c>Toy</c> got <c>Random2</c>'s numbers while the resolved record faithfully recorded
/// the request. That is the exact failure the resolved record exists to prevent, and no
/// test could have caught it, because there was nothing to compare a <c>Toy</c> run
/// against.
/// </para>
/// <para>
/// ⚠ <c>RANDOM1</c> is a lag-2 Fibonacci recurrence and its six states are six offsets
/// of one orbit. The port reproduces it because the original has it, not because it is
/// worth using; nothing should be concluded from a run made with it.
/// </para>
/// </remarks>
public sealed class ProbeRun4
{
    /// <summary>The recipe of <c>PROBE4.dat</c>: <c>PROBE.dat</c> with GSV 2 → 1.</summary>
    internal static StructureRunConfiguration Configuration =>
        ProbeRun.Configuration with { Generator = GeneratorSelection.Toy, GeneratorWarmup = 6_000_000 };

    [Fact]
    public void EveryDrawCounterMatchesTheOracle()
    {
        var result = PropellantStructureModel.Run(Configuration);

        Assert.Equal(866, result.Printed["nfx"].Value);
        Assert.Equal(24987, result.Printed["nfy"].Value);
        Assert.Equal(815, result.Printed["nfq"].Value);
        Assert.Equal(3533, result.Printed["nfw"].Value);
        Assert.Equal(3560, result.Printed["nkarm"].Value);
    }

    /// <summary>
    /// Condition 6 — <c>Nkarm = 0</c>, a base particle that ended up surrounded by
    /// nothing — fires here and in neither of the other probes.
    /// </summary>
    /// <remarks>
    /// It fires once in twenty particles, and the report prints 0.05 = 1/FI, which also
    /// confirms the second divisor on a run where the count is not zero. The archived
    /// runs are large enough that this condition is a rounding error in their statistics;
    /// on twenty particles it is a branch either taken or not.
    /// </remarks>
    [Fact]
    public void EveryConditionCounterMatchesTheOracle()
    {
        var state = Simulate();
        const int n = 20;

        // console output of the same run
        Assert.Equal(0, state.Conditions[1]);
        Assert.Equal(0, state.Conditions[2]);
        Assert.Equal(550, state.Conditions[3]);
        Assert.Equal(19565, state.Conditions[4]);
        Assert.Equal(219, state.Conditions[5]);
        Assert.Equal(1, state.Conditions[6]);
        Assert.Equal(0, state.Conditions[7]);
        Assert.Equal(57, state.Conditions[8]);
        Assert.Equal(0, state.Conditions[9]);

        // .m file, lines 47-49 and 51-52
        Assert.Equal(13.75, (double)state.Conditions[3] / (state.Fi + n), 6);
        Assert.Equal(489.125, (double)state.Conditions[4] / (state.Fi + n), 6);
        Assert.Equal(5.475, (double)state.Conditions[5] / (state.Fi + n), 6);
        Assert.Equal(5.0000001e-02, (double)state.Conditions[6] / state.Fi, 6);
        Assert.Equal(2.85, (double)state.Conditions[8] / state.Fi, 6);
    }

    [Fact]
    public void EveryDerivedScalarMatchesTheOracle()
    {
        var compare = new ProbeComparison(PropellantStructureModel.Run(Configuration));

        compare.Relative("epsx1", 4.4364929e-03);
        compare.Relative("epsx2", 2.7559996e-03);
        compare.Relative("epsx3", 6.7520142e-04);
        compare.Relative("epsx4", 2.1649599e-03);
        compare.Relative("epsx5", 2.8927922e-03);
        compare.Relative("epsx6", 7.4337125e-03);
        compare.Relative("eps_all_dok", 1.0223187e-02);
        compare.Relative("eps_dok_base", 2.5865447e-03);
        compare.Relative("eps_dok_surrounding", 1.0533137e-02);
        compare.Relative("eps_pocket_distribution", 0.1315349);
        compare.Relative("eps_pocket_moment4", 0.1062850);
        compare.Relative("eps_pocket_moment3", 7.7491499e-02);
        compare.Relative("gap_coefficient", 2.349296);
        compare.Relative("bridges_per_particle", 4.600000);
        compare.Relative("pocket_bridge_ratio", 7.272083);
        compare.Relative("jammed_fraction", 4.0596411e-02);
        compare.Relative("da_coef", 0.7389501);

        compare.Amplified("zkarm", 0.5713391);
        compare.Amplified("zkarm_cor1", 0.5713391);
        compare.Amplified("zkarm_cor2", 0.5637233);

        compare.Absolute("dqmkm1", 0.1472, 5e-5);
        compare.Absolute("dqmkm2", 0.4922, 5e-5);
        compare.Absolute("fine_oxidiser_fraction", 0.0, 0.0);

        // ⚠ Dok43a and Dok43sd are analytic — they read the recipe, not the draws — so
        // they are the same here as in every probe with this recipe. That they do not
        // move when the generator changes is itself the check.
        compare.Micrometres("dok43_analytic", 189.57);
        compare.Micrometres("dok43_sd", 58.87);
        compare.Micrometres("dok43_all_v1", 187.63);
        compare.Micrometres("dok43_all_v2", 187.59);
        compare.Micrometres("dok43_base", 189.08);
        compare.Micrometres("dok43_surrounding", 187.57);
        compare.Micrometres("dok_max", 315.00);
        compare.Micrometres("dkarm43_v1", 154.24);
        compare.Micrometres("dkarm43_v2", 154.20);
        compare.Micrometres("dkarm43_sd", 47.00);
        compare.Micrometres("dkarm43_cor1", 149.36);
        compare.Micrometres("dkarm43_sd_cor1", 42.03);
        compare.Micrometres("dkarm43_cor2", 154.48);
        compare.Micrometres("dkarm43_sd_cor2", 37.25);
        compare.Micrometres("dkarm10", 88.81);
        compare.Micrometres("dkarm10_cor", 88.31);
        compare.Micrometres("dagg43_v1", 113.98);
        compare.Micrometres("dagg43_v2", 113.95);
        compare.Micrometres("dagg43_cor1", 110.37);
        compare.Micrometres("dagg43_cor2", 114.15);

        compare.Verify();
    }

    /// <summary>
    /// The generator selection reaches the kernel — a run asking for <c>Toy</c> does not
    /// quietly get <c>Random2</c>.
    /// </summary>
    /// <remarks>
    /// This is the regression test for the defect the fixture exposed, and it is stated
    /// as a difference rather than as a value so it keeps meaning if either generator's
    /// numbers ever change: the two generators must not produce the same run.
    /// </remarks>
    [Fact]
    public void TheGeneratorSelectionActuallySelectsAGenerator()
    {
        var toy = PropellantStructureModel.Run(Configuration);
        var random2 = PropellantStructureModel.Run(ProbeRun.Configuration);

        Assert.NotEqual(toy.Printed["nfx"].Value, random2.Printed["nfx"].Value);
        Assert.NotEqual(toy.Printed["nkarm"].Value, random2.Printed["nkarm"].Value);
        Assert.NotEqual(toy.Printed["zkarm"].Value, random2.Printed["zkarm"].Value);
    }

    /// <summary>
    /// <c>gsv = 3</c> is refused, and refused for a reason that will not go stale.
    /// </summary>
    [Fact]
    public void TheSelfSeedingGeneratorIsRefusedRatherThanApproximated()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => PropellantStructureModel.Run(
                ProbeRun.Configuration with { Generator = GeneratorSelection.SystemSeeded }));

        Assert.Contains("outside the program", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>The literals above are the oracle's.</summary>
    [Fact]
    public void TheExpectationsAreTheOnesTheOracleFilePrints()
    {
        var report = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe4.m"));

        // ⚠ The header prints NNZ through an E9.3 edit descriptor although it is an
        // INTEGER, so 6000000 comes out as its own bit pattern read as a denormal. The
        // value the run used is in PROBE4.dat, not here - and this asserts the misprint
        // so that a reader who meets 0.841E-38 elsewhere knows what it is.
        Assert.Contains(
            "% Nfr =  3; JZ = 1; Cycles =  1; N =     20; GSV = 1; NNZ =0.841E-38;",
            report,
            StringComparison.Ordinal);

        Assert.Contains("NFX =                   866 ;  NFY =                 24987 ;", report, StringComparison.Ordinal);
        Assert.Contains("NFQ =                   815 ;  NFW =                  3533 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nkarm =                  3560 ;", report, StringComparison.Ordinal);
        Assert.Contains("6) Nkarm = 0       :  5.0000001E-02", report, StringComparison.Ordinal);
        Assert.Contains("Zkarm =   0.5713391", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// The misprinted <c>NNZ</c>, decoded — the report's <c>0.841E-38</c> is the integer
    /// 6 000 000 seen as a REAL*4.
    /// </summary>
    /// <remarks>
    /// Worth a test rather than a comment because it is the only evidence that the value
    /// this fixture uses is the value the run used. If the decoding were wrong, the six
    /// generator states would be cut at the wrong offsets and every number above would
    /// miss — so this and the counters stand or fall together, and this one says why.
    /// </remarks>
    [Fact]
    public void TheReportsPrintedWarmupIsTheIntegerSeenAsAFloat()
    {
        var asFloat = BitConverter.Int32BitsToSingle(6_000_000);

        Assert.Equal(0.841e-38, asFloat, 0.0005e-38);
        Assert.Equal(6_000_000, Configuration.GeneratorWarmup);
    }

    private static RunState Simulate()
    {
        var simulation = new StructureSimulation(
            Configuration,
            RandomStreamSet.Toy(Configuration.GeneratorWarmup));
        simulation.Simulate();
        return simulation.State;
    }
}
