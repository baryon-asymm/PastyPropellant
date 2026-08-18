using PastyPropellant.PropellantStructure.Results;
using Xunit.Abstractions;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// Where the constants in <c>data/propellants*.json</c> came from, replayed.
/// </summary>
/// <remarks>
/// <para>
/// The upper repository quotes three pocket mass fractions — 0.70 for
/// <c>Bas_0</c>/<c>Bas_1</c>/<c>Bas_2</c>, 0.57 for <c>Bas_3</c>, 0.56 for
/// <c>Bas_4</c> — as literals with no derivation attached. They are outputs of this
/// model, and these tests identify the runs that produced them and replay those runs.
/// </para>
/// <para>
/// The identification is arithmetic, not guesswork. This model has <b>one</b>
/// oxidiser and the recipe has two, and they reconcile as AP + octogen:
/// 0.30 + 0.2827 = 0.5827 against the archive's 0.583, with metal 0.2073 against
/// 0.207. Inside that oxidiser the octogen is 0.2827/0.5827 = 0.4852 — the
/// <c>0.486</c> coarse fraction standing in every run of the family — and the AP is
/// the remaining 0.5148, split by the recipe's <c>large_particles_fraction</c>:
/// 0.35 × 0.5148 = 0.180 and 0.65 × 0.5148 = 0.335 for <c>Bas_2</c>, against
/// <c>hp1</c>'s 0.180 and 0.334. Three decimals on two independent numbers is not a
/// coincidence.
/// </para>
/// <para>
/// ⚠ These are also two of the fifteen exact-input runs L2 never replays, so they
/// carry a second load: the claim that those fifteen take no branch the replayed six
/// do not was an <i>argument</i>, and asserting their counters here turns it into a
/// measurement. The counters are what makes that work — one divergent decision
/// desynchronises the draw sequence and no counter survives.
/// </para>
/// </remarks>
public sealed class PropellantJsonProvenanceTests(ITestOutputHelper output)
{
    /// <summary><c>Bas_0</c>, <c>Bas_1</c>, <c>Bas_2</c> — 65 % coarse AP. ~10 min.</summary>
    [Fact]
    [Trait("Category", "LongRunning")]
    public void TheBimodalRecipeIsRunHp1() => Replay("hp1", 0.70);

    /// <summary><c>Bas_3</c> — all-fine AP, 10–50 µm. ~1.5 h.</summary>
    [Fact]
    [Trait("Category", "LongRunning")]
    public void TheFineRecipeIsRunHp1050() => Replay("hp1050", 0.57);

    /// <summary><c>Bas_4</c> — all-coarse AP, 113–180 µm. ~1.5 h.</summary>
    [Fact]
    [Trait("Category", "LongRunning")]
    public void TheCoarseRecipeIsRunHp180() => Replay("hp180", 0.56);

    /// <summary>
    /// Replays one run and reports every branch of the two published quantities
    /// against the value the recipe file carries.
    /// </summary>
    /// <remarks>
    /// The assertions are on the archive, not on the recipe file. Which branch the
    /// recipe file quotes is a question about how the constant was transcribed, and a
    /// test that demanded a particular answer would be asserting the transcription
    /// rather than measuring it — so the comparison against <paramref name="quoted"/>
    /// is printed, and only the reproduction is asserted.
    /// </remarks>
    private void Replay(string id, double quoted)
    {
        var run = ReferenceRuns.ById(id);
        Assert.True(run.HasExactInput, $"{id} has no surviving exact input; integers cannot be compared.");

        var started = DateTime.UtcNow;
        var result = PropellantStructureModel.Run(run.ToConfiguration());
        output.WriteLine($"{id}: {(DateTime.UtcNow - started).TotalMinutes:F1} min");

        var problems = new List<string>();

        // First group: the integers. Nothing here has a tolerance.
        var counters = new (string Name, long Value)[]
        {
            ("nfx", result.Diagnostics.BaseParticleDraws),
            ("nfy", result.Diagnostics.SurroundingParticleDraws),
            ("nfq", result.Diagnostics.PocketSizeDraws),
            ("nfw", result.Diagnostics.BridgeDraws),
            ("nkarm", result.Diagnostics.PocketsTotal),
        };

        output.WriteLine(string.Empty);
        foreach (var (name, value) in counters)
        {
            var expected = (long)run.Scalars[name];
            output.WriteLine($"  {name,-6} {value,14} oracle {expected,14} {(value == expected ? "=" : "DIFFER")}");
            if (value != expected)
            {
                problems.Add($"{name}: {value}, oracle {expected}.");
            }
        }

        // Then the two published quantities, every branch of each. The pocket mass
        // fraction reads a bridge volume, so it carries the loosened 1e-5 the whole
        // tree gives that family; the diameters print F7.2 on micrometres.
        output.WriteLine(string.Empty);
        output.WriteLine("  pocket mass fraction        port        oracle     vs recipe file");
        foreach (var (branch, key) in Branches("zkarm", "zkarm_cor1", "zkarm_cor2"))
        {
            var mine = result.PocketMassFraction[branch].Value;
            var oracle = run.Scalars[key];
            output.WriteLine($"  {key,-12} {mine,12:F6} {oracle,12:F6}   {quoted:F2} -> {(Math.Abs(mine - quoted) < 0.005 ? "MATCHES" : "no")}");
            Check(key, mine, oracle, Math.Abs(oracle) * 1e-5);
        }

        output.WriteLine(string.Empty);
        output.WriteLine("  pocket diameter, um  (mass mean, D43)");
        foreach (var (branch, key) in Branches("dkarm43_v2", "dkarm43_cor1", "dkarm43_cor2"))
        {
            var mine = result.PocketDiameter43[branch].Value.Micrometers;
            var oracle = run.Scalars[key];
            output.WriteLine($"  {key,-14} {mine,10:F2} {oracle,10:F2}");
            Check(key, mine, oracle, 0.005);
        }

        // The number mean is a different question about the same population and the
        // report prints it separately: D43 weights a pocket by its volume, so on a wide
        // distribution the two are far apart (hp1050: 222.29 against 33.34) and quoting
        // one where the other is meant is a factor-of-seven error, not a rounding one.
        output.WriteLine(string.Empty);
        output.WriteLine("  pocket diameter, um  (number mean, D10)");
        foreach (var (branch, key) in Branches("dkarm10", "dkarm10_cor", "dkarm10_cor"))
        {
            if (branch == Correction.Variant2)
            {
                // The report has only two D10 branches; Variant2 shares Variant1's.
                continue;
            }

            var mine = result.PocketDiameter10[branch].Value.Micrometers;
            var oracle = run.Scalars[key];
            output.WriteLine($"  {key,-14} {mine,10:F2} {oracle,10:F2}");
            Check(key, mine, oracle, 0.005);
        }

        // The particles the pocket is made of. `dok43_surrounding` is the one the result
        // type exposes as PocketWallDiameter43 - a pocket is the void between the base
        // particle and the neighbours packed around it, and it is the neighbours that
        // bound it. The other two are printed alongside because the original prints all
        // three, and because base-vs-surrounding agreeing is what says the packing did
        // not sort the fractions by role.
        output.WriteLine(string.Empty);
        output.WriteLine("  oxidiser diameter, um  (mass mean, D43)");
        foreach (var key in new[]
                 { "dok43_surrounding", "dok43_base", "dok43_all_v1", "dok43_all_v2", "dok43_analytic" })
        {
            var mine = key == "dok43_surrounding"
                ? result.PocketWallDiameter43.Value.Micrometers
                : result.PrintedLengths[key].Value.Micrometers;
            var oracle = run.Scalars[key];
            output.WriteLine($"  {key,-18} {mine,10:F2} {oracle,10:F2}");
            Check(key, mine, oracle, 0.005);
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        return;

        void Check(string name, double mine, double oracle, double allowed)
        {
            if (Math.Abs(mine - oracle) > allowed)
            {
                problems.Add($"{name}: {mine:R}, oracle {oracle:R}, allowed {allowed:E2}.");
            }
        }
    }

    private static IEnumerable<(Correction Branch, string Key)> Branches(string none, string one, string two) =>
        [(Correction.None, none), (Correction.Variant1, one), (Correction.Variant2, two)];
}
