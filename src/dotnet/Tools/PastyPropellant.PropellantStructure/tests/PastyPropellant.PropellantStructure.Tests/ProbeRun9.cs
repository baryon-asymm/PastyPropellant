using PastyPropellant.PropellantStructure.Configuration;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// <see cref="ProbeRun"/>'s recipe with <c>eta = 0.25</c> — the agglomerated-oxide
/// share, and the last setting the port carried with no reference behind it.
/// </summary>
/// <remarks>
/// <para>
/// <c>eta</c> is not a branch; it enters one two-line formula (1015–1016) that scales
/// the agglomerate mass, and from there only <c>da_coef</c> and the four
/// <c>Dagg43</c> variants. The archive covers two values, 0 and 0.5, and the report
/// never prints <c>eta</c> itself, so the two are not distinguishable from the file —
/// they were read out of the run notes. Two points also fit almost anything: the
/// formula has <c>eta</c> in three places and is a cube root of a ratio of two
/// linear-in-<c>eta</c> factors. A third point off the ends is what tests it.
/// </para>
/// <para>
/// The fixture is worth as much for what it leaves alone as for what it moves.
/// <c>probe9.m</c> differs from <c>probe.m</c> in exactly five printed lines — the
/// five below — and is byte-identical everywhere else, counters and arrays included.
/// A port that let <c>eta</c> leak into the packing rather than into the agglomerate
/// would be caught by the equality half, not by the five numbers.
/// </para>
/// </remarks>
public sealed class ProbeRun9
{
    /// <summary><c>PROBE.dat</c> with <c>eta</c> answered as 0.25 at menu item 14.</summary>
    internal static StructureRunConfiguration Configuration =>
        ProbeRun.Configuration with { AgglomeratedOxideShare = 0.25 };

    /// <summary>The five quantities eta reaches, against the oracle.</summary>
    [Fact]
    public void TheAgglomerateScaleMatchesTheOracle()
    {
        var compare = new ProbeComparison(PropellantStructureModel.Run(Configuration));

        compare.Relative("da_coef", 0.7484065);
        compare.Micrometres("dagg43_v1", 111.18);
        compare.Micrometres("dagg43_v2", 111.17);
        compare.Micrometres("dagg43_cor1", 110.34);
        compare.Micrometres("dagg43_cor2", 113.64);
        compare.Verify();
    }

    /// <summary>
    /// Everything else is bit-identical to the run with <c>eta = 0</c>, because
    /// <c>eta</c> is applied after the structure is built and not while it is built.
    /// </summary>
    [Fact]
    public void EtaMovesTheAgglomerateScaleAndNothingElse()
    {
        var plain = PropellantStructureModel.Run(ProbeRun.Configuration);
        var scaled = PropellantStructureModel.Run(Configuration);

        string[] reached = ["da_coef", "dagg43_v1", "dagg43_v2", "dagg43_cor1", "dagg43_cor2"];

        Assert.Equal(plain.Diagnostics, scaled.Diagnostics);

        var problems = new List<string>();
        foreach (var (name, value) in plain.Printed)
        {
            var moved = Math.Abs(scaled.Printed[name].Value - value.Value) > 0.0;
            if (moved != reached.Contains(name))
            {
                problems.Add($"{name}: {(moved ? "moved" : "held")} at eta = 0.25, expected the opposite.");
            }
        }

        foreach (var (name, value) in plain.PrintedLengths)
        {
            var moved = Math.Abs(scaled.PrintedLengths[name].Value.Meters - value.Value.Meters) > 0.0;
            if (moved != reached.Contains(name))
            {
                problems.Add($"{name}: {(moved ? "moved" : "held")} at eta = 0.25, expected the opposite.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// The answer sequence, and the correction that came with generating this probe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ The sequences recorded for probes 6, 7 and 8 each carried a spurious <c>y</c>
    /// after the file name, and would have re-run the original <b>with default
    /// parameters</b> rather than with the setting the probe is named for — the
    /// <c>y</c> answers «use default parameters?» and skips the menu the next answers
    /// were meant for. The reports themselves are genuine; it was the recipe for
    /// re-making them that was wrong, and nothing caught it because the tests compared
    /// the file to a literal instead of running it.
    /// </para>
    /// <para>
    /// It surfaced here: the same sequence with <c>14</c> and <c>0.25</c> produced a
    /// report byte-identical to <c>probe.m</c>, which for a setting that moves five
    /// numbers is a result that cannot be right. All four sequences are now the
    /// corrected form, and each was checked by regenerating its report under wine and
    /// diffing it against the file in the tree — all four reproduce byte for byte.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheAnswerSequenceIsKeptWithTheReport()
    {
        var answers = File.ReadAllLines(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe9.answers.txt"));

        Assert.Equal(["PROBE", "n", "14", "0.25", "0"], answers);

        // No probe answers "use default parameters?" with y: a probe that used the
        // defaults would be the baseline probe.
        foreach (var name in (string[])["probe6", "probe7", "probe8", "probe9"])
        {
            var other = File.ReadAllLines(
                RepositoryPaths.Resolve("reference", "propstruct", "probes", $"{name}.answers.txt"));
            Assert.Equal("n", other[1]);
        }
    }
}
