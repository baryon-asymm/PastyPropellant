using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// <see cref="ProbeRun"/>'s recipe with the coarsest fraction excluded from forming
/// pockets — <c>SFR = [1 1 0]</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two archived runs carry a non-trivial <c>SFR</c>, so the flag itself is not new. What
/// is new is that this fixture is small enough to trace and that it lights up the two
/// conditions nothing else reaches: <b>7</b> (<c>Nmkm &lt; 2</c>) fires five times and
/// <b>9</b> (<c>Nkarm/Nmkm &gt; max</c>) twice. Between this probe and
/// <see cref="ProbeRun7"/>, eight of the nine conditions now have a fixture where they
/// are not zero; only condition 1 (<c>Dok &gt; Dmax</c>) is still unexercised, and it
/// cannot fire while the fractions are sampled inside their own declared bounds.
/// </para>
/// <para>
/// ⚠ Seven of those eight are exercised by a <b>run of the port</b>. Condition 2 is not:
/// its only fixture is <see cref="ProbeRun7"/>, whose input the port refuses, so the
/// condition has an oracle and no replay. That leaves this probe carrying the diverted
/// mass path on its own as far as the port is concerned.
/// </para>
/// <para>
/// The excluded fraction's whole mass share reappears as homogenised oxidiser:
/// <c>fineoxy_fr = 0.486</c>, which is <c>Gfr(3)</c> exactly. It is the only check on
/// where the diverted mass goes that the port itself runs, and it lands on a round
/// number, which is what makes it worth having: the second term of
/// <c>gdokns + Gdokleft/GGG</c> is zero in 41 of the 43 archived runs, so a port that
/// dropped it would stay green almost everywhere.
/// </para>
/// <para>
/// ⚠ Its input is <c>PROBE8.dat</c> — <c>PROBE.dat</c> plus the two lines the original
/// reads only when told to — <b>and</b> the answers in <c>probe8.answers.txt</c>, since
/// the extra lines are read only after answering <c>[12]</c> with <c>y</c>. Neither half
/// works alone.
/// </para>
/// </remarks>
public sealed class ProbeRun8
{
    /// <summary><c>PROBE8.dat</c>: the third fraction takes no part in forming pockets.</summary>
    internal static StructureRunConfiguration Configuration
    {
        get
        {
            var fractions = ProbeRun.Configuration.Fractions;
            return ProbeRun.Configuration with
            {
                Fractions =
                [
                    fractions[0],
                    fractions[1],
                    fractions[2] with { FormsPockets = false },
                ],
            };
        }
    }

    [Fact]
    public void EveryDrawCounterMatchesTheOracle()
    {
        var result = PropellantStructureModel.Run(Configuration);

        Assert.Equal(399, result.Printed["nfx"].Value);
        Assert.Equal(17906, result.Printed["nfy"].Value);
        Assert.Equal(109, result.Printed["nfq"].Value);
        Assert.Equal(1638, result.Printed["nfw"].Value);
        Assert.Equal(2500, result.Printed["nkarm"].Value);
    }

    /// <summary>Conditions 7 and 9, on the only run in the tree where they are not zero.</summary>
    [Fact]
    public void EveryConditionCounterMatchesTheOracle()
    {
        var state = Simulate();
        const int n = 20;

        // console output of the same run
        Assert.Equal(0, state.Conditions[1]);
        Assert.Equal(0, state.Conditions[2]);
        Assert.Equal(246, state.Conditions[3]);
        Assert.Equal(14283, state.Conditions[4]);
        Assert.Equal(96, state.Conditions[5]);
        Assert.Equal(0, state.Conditions[6]);
        Assert.Equal(5, state.Conditions[7]);
        Assert.Equal(1, state.Conditions[8]);
        Assert.Equal(2, state.Conditions[9]);

        // .m file, lines 46-48 and 50-53. Note 7, 8 and 9 divide by FI and 3-5 by FI+N;
        // this is the only run where all three of the last four are non-zero at once, so
        // it is the only one that checks that divisor on more than a single counter.
        Assert.Equal(6.15, (double)state.Conditions[3] / (state.Fi + n), 6);
        Assert.Equal(357.075, (double)state.Conditions[4] / (state.Fi + n), 6);
        Assert.Equal(2.4, (double)state.Conditions[5] / (state.Fi + n), 6);
        Assert.Equal(0.25, (double)state.Conditions[7] / state.Fi, 6);
        Assert.Equal(5.0000001e-02, (double)state.Conditions[8] / state.Fi, 6);
        Assert.Equal(0.1, (double)state.Conditions[9] / state.Fi, 6);
    }

    /// <summary>
    /// The excluded fraction's mass share becomes homogenised oxidiser exactly, to the
    /// digit.
    /// </summary>
    [Fact]
    public void TheExcludedFractionBecomesHomogenisedOxidiserExactly()
    {
        var result = PropellantStructureModel.Run(Configuration);
        var excluded = Configuration.Fractions[2].MassFraction;

        Assert.Equal(0.486, excluded);
        Assert.Equal(0.4860000, result.Printed["fine_oxidiser_fraction"].Value, 5e-7);
    }

    [Fact]
    public void EveryDerivedScalarMatchesTheOracle()
    {
        var compare = new ProbeComparison(PropellantStructureModel.Run(Configuration));

        compare.Relative("epsx1", 4.1413009e-02);
        compare.Relative("epsx2", 4.0232837e-02);
        compare.Relative("epsx3", 9.2208385e-04);
        compare.Relative("epsx4", 8.4173679e-04);
        compare.Relative("epsx5", 6.5205514e-02);
        compare.Relative("epsx6", 1.7599523e-02);
        compare.Relative("eps_all_dok", 1.1910738e-02);
        compare.Relative("eps_dok_base", 7.2440825e-02);
        compare.Relative("eps_dok_surrounding", 1.0610709e-02);
        compare.Relative("eps_pocket_distribution", 0.1231227);
        compare.Relative("eps_pocket_moment4", 9.6535206e-02);
        compare.Relative("eps_pocket_moment3", 7.6420940e-02);
        compare.Relative("gap_coefficient", 2.624473);
        compare.Relative("bridges_per_particle", 4.150000);
        compare.Relative("pocket_bridge_ratio", 12.13294);
        compare.Relative("jammed_fraction", 4.3351367e-02);

        // 0.6363022 against 0.7389501 — the largest move of this coefficient in any
        // probe, and it should be: a third of the oxidiser has left the pockets.
        compare.Relative("da_coef", 0.6363022);

        compare.Amplified("zkarm", 0.7989844);
        compare.Amplified("zkarm_cor1", 0.7989844);
        compare.Amplified("zkarm_cor2", 0.8034456);

        compare.Absolute("dqmkm1", 0.1690, 5e-5);
        compare.Absolute("dqmkm2", 0.5831, 5e-5);

        // ⚠ Analytic and so unmoved: Dok43a and Dok43sd read the recipe's fractions,
        // which SFR does not change - it changes only which of them build pockets.
        compare.Micrometres("dok43_analytic", 189.57);
        compare.Micrometres("dok43_sd", 58.87);
        compare.Micrometres("dok43_all_v1", 191.83);
        compare.Micrometres("dok43_all_v2", 191.90);
        compare.Micrometres("dok43_base", 143.35);
        compare.Micrometres("dok43_surrounding", 144.22);
        compare.Micrometres("dok_max", 315.00);
        compare.Micrometres("dkarm43_v1", 236.03);
        compare.Micrometres("dkarm43_v2", 236.11);
        compare.Micrometres("dkarm43_sd", 57.65);
        compare.Micrometres("dkarm43_cor1", 245.17);
        compare.Micrometres("dkarm43_sd_cor1", 57.95);
        compare.Micrometres("dkarm43_cor2", 247.14);
        compare.Micrometres("dkarm43_sd_cor2", 56.22);
        compare.Micrometres("dkarm10", 143.56);
        compare.Micrometres("dkarm10_cor", 149.68);
        compare.Micrometres("dagg43_v1", 150.18);
        compare.Micrometres("dagg43_v2", 150.24);
        compare.Micrometres("dagg43_cor1", 156.01);
        compare.Micrometres("dagg43_cor2", 157.26);

        compare.Verify();
    }

    /// <summary>The literals above are the oracle's, and the input file is the one named.</summary>
    [Fact]
    public void TheExpectationsAreTheOnesTheOracleFilePrints()
    {
        var report = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe8.m"));

        Assert.Contains("% Input filename: PROBE8.dat", report, StringComparison.Ordinal);
        Assert.Contains("NFX =                   399 ;  NFY =                 17906 ;", report, StringComparison.Ordinal);
        Assert.Contains("NFQ =                   109 ;  NFW =                  1638 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nkarm =                  2500 ;", report, StringComparison.Ordinal);
        Assert.Contains("fineoxy_fr =  0.4860000     ;", report, StringComparison.Ordinal);
        Assert.Contains("7) Nmkm < 2        :  0.2500000", report, StringComparison.Ordinal);
        Assert.Contains("9) Nkarm/Nmkm > max:  0.1000000", report, StringComparison.Ordinal);

        // ⚠ The .dat carries the SFR line, but the original reads it only after [12] is
        // answered y. Both halves of the input are kept, and both are checked here.
        var input = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "PROBE8.dat"));
        Assert.Contains("    1 1 0", input, StringComparison.Ordinal);

        var answers = File.ReadAllLines(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe8.answers.txt"));
        Assert.Equal(["PROBE8", "n", "12", "y", "0"], answers);
    }

    private static RunState Simulate()
    {
        var simulation = new StructureSimulation(Configuration, RandomStreamSet.Historical());
        simulation.Simulate();
        return simulation.State;
    }
}
