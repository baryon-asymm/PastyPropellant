using PastyPropellant.PropellantStructure.Configuration;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// <see cref="ProbeRun"/>'s recipe with <c>Dmin = 60 µm</c> — a configuration the
/// original runs and the port refuses. The fixture exists to show what the refusal
/// costs and what it buys.
/// </summary>
/// <remarks>
/// <para>
/// Every archived run and every other probe sets <c>Dmin = Di = 10 µm</c>, the lower
/// bound of the finest fraction, so condition 2 (<c>Dok &lt; Dmin</c>) never fires and no
/// mass is ever diverted. This fixture was taken to close that gap, and it does close it
/// — <c>probe7.m</c> is a complete, well-formed report with condition 2 firing 16 249
/// times and <c>fineoxy_fr = 1.0191291E-02</c> instead of exactly zero.
/// </para>
/// <para>
/// ⚠ <b>But the port rejects the configuration up front</b>, in
/// <c>StructureConfigurationValidator.CheckGrids</c>, on the ground that
/// <c>pdoksmall</c> holds the below-cutoff share per histogram cell and so cannot
/// express a cutoff spanning more than one cell. The oracle <b>confirms</b> that
/// reasoning rather than refuting it: in <c>probe7.m</c> the first 22 cells of
/// <c>pdoksmall</c> are <c>0.569E-38</c> — uninitialised memory — against 2 such cells
/// in <c>probe.m</c>. See
/// <see cref="TheOriginalRunsThisAndFillsPdoksmallWithUninitialisedMemory"/>.
/// </para>
/// <para>
/// ⚠ <b>This is a decision left open, not one taken here.</b> The guard is deliberate
/// and its rationale is now checked, so it is not removed; but it does block a run the
/// original performs, and thirteen of the fourteen arrays and all 51 scalars of that run
/// are perfectly well-formed. Loosening it — running the configuration and marking only
/// <c>pdoksmall</c> as unusable — is a design change with a cost either way, and it
/// belongs to whoever owns the tree. The numbers are recorded in
/// <c>reference/propstruct/probes/probe7.m</c> so that either choice can be made on
/// evidence.
/// </para>
/// </remarks>
public sealed class ProbeRun7
{
    /// <summary><c>PROBE.dat</c> with <c>Dok_min</c> answered as 60 µm.</summary>
    internal static StructureRunConfiguration Configuration =>
        ProbeRun.Configuration with { MinimumParticleSize = Length.FromMicrometers(60) };

    /// <summary>The port refuses it, before drawing anything.</summary>
    [Fact]
    public void ThePortRefusesACutoffWiderThanTheHistogramCell()
    {
        var exception = Assert.Throws<StructureConfigurationException>(
            () => PropellantStructureModel.Run(Configuration));

        Assert.Contains(
            exception.Problems,
            problem => problem.Contains(nameof(Configuration.MinimumParticleSize), StringComparison.Ordinal)
                && problem.Contains("histogram cell", StringComparison.Ordinal));
    }

    /// <summary>
    /// The original does not refuse it — and the guard's stated reason is visible in its
    /// output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the whole value of the fixture. The guard was written from reading the
    /// source: <c>pdoksmall</c> is one cell of the particle histogram wide, so a cutoff
    /// above one cell leaves whole cells with nothing to hold. That was an argument.
    /// Now it is a measurement: 22 leading cells of <c>0.569E-38</c>, the denormal that
    /// marks memory the original never wrote, against 2 in the baseline probe.
    /// </para>
    /// <para>
    /// ⚠ The denormal is not a number the original computed. <c>reference/…/runs.json</c>
    /// records <c>pdoksmall_is_subnormal</c> for the same reason, and a re-run of the
    /// original under a different loader gives a different denormal — so these cells
    /// cannot be reproduced by anything, port or original.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheOriginalRunsThisAndFillsPdoksmallWithUninitialisedMemory()
    {
        var report = File.ReadAllText(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe7.m"));

        // it ran, and it ran to completion
        Assert.Contains("  Dmin =          60 ; % mkm", report, StringComparison.Ordinal);
        Assert.Contains("  Di =          10 ; % mkm", report, StringComparison.Ordinal);
        Assert.Contains("NFX =                   885 ;  NFY =                 18283 ;", report, StringComparison.Ordinal);
        Assert.Contains("Nkarm =                  1730 ;", report, StringComparison.Ordinal);

        // the branch it was taken for
        Assert.Contains("2) Dok < Dmin      :   406.2250", report, StringComparison.Ordinal);
        Assert.Contains("fineoxy_fr =  1.0191291E-02 ;", report, StringComparison.Ordinal);

        // and the cost the guard names: 22 leading cells of uninitialised memory
        var leadingDenormals = LeadingDenormalCells(report);
        var baseline = LeadingDenormalCells(
            File.ReadAllText(RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe.m")));

        Assert.Equal(2, baseline);
        Assert.True(
            leadingDenormals > baseline,
            $"pdoksmall opens with {leadingDenormals} untouched cells here and {baseline} in the baseline. "
            + "If that is no longer so, the guard in CheckGrids has lost the evidence behind it.");
        Assert.Equal(22, leadingDenormals);
    }

    /// <summary>
    /// The answer sequence that produced the report, kept because the report does not
    /// determine its own input.
    /// </summary>
    [Fact]
    public void TheAnswerSequenceIsKeptWithTheReport()
    {
        var answers = File.ReadAllLines(
            RepositoryPaths.Resolve("reference", "propstruct", "probes", "probe7.answers.txt"));

        Assert.Equal(["PROBE", "n", "1", "60", "0"], answers);
    }

    /// <summary>How many cells <c>pdoksmall</c> opens with that the original never wrote.</summary>
    private static int LeadingDenormalCells(string report)
    {
        var start = report.IndexOf("pdoksmall = [", StringComparison.Ordinal);
        Assert.True(start >= 0, "the report carries no pdoksmall array.");

        var end = report.IndexOf("];", start, StringComparison.Ordinal);
        var body = report[(start + "pdoksmall = [".Length)..end]
            .Replace("...", " ", StringComparison.Ordinal);

        var cells = 0;
        foreach (var token in body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            // 0.569E-38 and its neighbours: anything below the smallest normal single.
            if (!double.TryParse(token, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var value)
                || Math.Abs(value) >= 1.1754944e-38)
            {
                break;
            }

            cells++;
        }

        return cells;
    }
}
