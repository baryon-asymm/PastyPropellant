using System.Globalization;
using System.Text.RegularExpressions;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Reports.Pdf;
using UnitsNet;

namespace ParametricCombustionModel.ReportMaking.Tests;

/// <summary>
/// Coverage for the heat-flux share arithmetic in <see cref="FlameStructureReport"/>.
///
/// The report's own contract is that the feedback to the surface splits exactly into three additive
/// pathways — out-of-skeleton kinetic, skeleton, and diffusion — so the three printed shares sum to 100%.
/// That claim is what makes the page readable as a competing-flames verdict, and it is what these tests pin,
/// along with the pressure-averaged "mean shares" line.
/// </summary>
public class FlameStructureReportTests
{
    private static readonly Regex ShareLine = new(
        @"out-kin (?<out>[\d.]+)%, skeleton (?<skel>[\d.]+)%, diff (?<diff>[\d.]+)%",
        RegexOptions.Compiled);

    private static readonly Regex MeanShareLine = new(
        @"mean shares: out-kin (?<out>[\d.]+)%, skeleton (?<skel>[\d.]+)%, diffusion (?<diff>[\d.]+)%",
        RegexOptions.Compiled);

    /// <summary>
    /// Sets the three additive pathways at one point. The total is their sum, matching the solver's own
    /// invariant (ToSurfaceTotal = OutSkeleton + Skeleton + Diffusion).
    /// </summary>
    private static void SetHeatFluxes(
        OptimizationProblemByUnits problem,
        int pressure,
        double outSkeleton,
        double skeleton,
        double diffusion,
        bool burnRateIsFound = true)
    {
        problem.ProblemContextMatrix[0, pressure].PocketCombustionParams = new PocketCombustionParams
        {
            BurnRateIsFound = burnRateIsFound,
            OutSkeletonHeatFlux = HeatFlux.FromWattsPerSquareMeter(outSkeleton),
            SkeletonHeatFlux = HeatFlux.FromWattsPerSquareMeter(skeleton),
            DiffusionFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(diffusion),
            ToSurfaceTotalHeatFlux = HeatFlux.FromWattsPerSquareMeter(outSkeleton + skeleton + diffusion)
        };

        problem.ProblemContextMatrix[0, pressure].MixedCombustionParams = new MixedCombustionParams
        {
            BurnRateIsFound = burnRateIsFound,
            BurnRate = Speed.FromMetersPerSecond(0.003)
        };
    }

    private static GroupOptimizationResult MakeGroup(int pressureCount, out OptimizationProblemByUnits subject)
    {
        var pascals = Enumerable.Range(0, pressureCount).Select(i => 1e6 * (i + 1)).ToArray();
        subject = ReportFixture.MakeProblem([ReportFixture.MakePropellant("Bas_0")], pascals);
        return ReportFixture.MakeGroupResult(subject,
                                             ReportFixture.MakeProblem([], [1e6]),
                                             ReportFixture.MakeProblem([], [1e6]));
    }

    private static (double Out, double Skel, double Diff) ParseOnly(Regex pattern, IReadOnlyList<string> lines)
    {
        var matches = lines.Select(line => pattern.Match(line)).Where(m => m.Success).ToArray();
        Assert.True(matches.Length == 1, $"Expected exactly one matching line, found {matches.Length}.");

        return (double.Parse(matches[0].Groups["out"].Value, CultureInfo.InvariantCulture),
                double.Parse(matches[0].Groups["skel"].Value, CultureInfo.InvariantCulture),
                double.Parse(matches[0].Groups["diff"].Value, CultureInfo.InvariantCulture));
    }

    private static IReadOnlyList<string> Render(GroupOptimizationResult groupResult) =>
        ReportFixture.TextLines(new FlameStructureReport(groupResult).Transform());

    /// <summary>
    /// Fluxes of 1, 2 and 1 MW/m2 total 4, so the shares are 25%, 50% and 25% — and they sum to 100%, which
    /// is the invariant the page's whole reading depends on.
    /// </summary>
    [Fact]
    public void Shares_AreThePathwayFractionsOfTheTotalAndSumToOneHundredPercent()
    {
        var groupResult = MakeGroup(pressureCount: 1, out var subject);
        SetHeatFluxes(subject, 0, outSkeleton: 1e6, skeleton: 2e6, diffusion: 1e6);

        var (outShare, skelShare, diffShare) = ParseOnly(ShareLine, Render(groupResult));

        Assert.Equal(25.0, outShare, 1);
        Assert.Equal(50.0, skelShare, 1);
        Assert.Equal(25.0, diffShare, 1);
        Assert.Equal(100.0, outShare + skelShare + diffShare, 1);
    }

    /// <summary>Shares must stay fractions of the total regardless of the total's magnitude.</summary>
    [Theory]
    [InlineData(1e5)]
    [InlineData(1e6)]
    [InlineData(1e9)]
    public void Shares_SumToOneHundredPercentAtAnyTotalMagnitude(double scale)
    {
        var groupResult = MakeGroup(pressureCount: 1, out var subject);
        SetHeatFluxes(subject, 0, outSkeleton: 3.0 * scale, skeleton: 5.0 * scale, diffusion: 2.0 * scale);

        var (outShare, skelShare, diffShare) = ParseOnly(ShareLine, Render(groupResult));

        Assert.Equal(30.0, outShare, 1);
        Assert.Equal(50.0, skelShare, 1);
        Assert.Equal(20.0, diffShare, 1);
    }

    /// <summary>
    /// The mean shares line is the pressure-average of the per-point shares. Points at 20/50/30 and
    /// 40/30/30 average to 30/40/30.
    /// </summary>
    [Fact]
    public void MeanShares_AreThePressureAverageOfThePerPointShares()
    {
        var groupResult = MakeGroup(pressureCount: 2, out var subject);
        SetHeatFluxes(subject, 0, outSkeleton: 2e6, skeleton: 5e6, diffusion: 3e6);
        SetHeatFluxes(subject, 1, outSkeleton: 4e6, skeleton: 3e6, diffusion: 3e6);

        var (outShare, skelShare, diffShare) = ParseOnly(MeanShareLine, Render(groupResult));

        Assert.Equal(30.0, outShare, 1);
        Assert.Equal(40.0, skelShare, 1);
        Assert.Equal(30.0, diffShare, 1);
    }

    /// <summary>
    /// A point with no surface heat feedback is marked and must not enter the average. Here the one usable
    /// point is 20/50/30; averaging it against a zeroed point would give 10/25/15 instead.
    /// </summary>
    [Fact]
    public void MeanShares_AverageOnlyOverPointsThatHadSurfaceHeatFeedback()
    {
        var groupResult = MakeGroup(pressureCount: 2, out var subject);
        SetHeatFluxes(subject, 0, outSkeleton: 2e6, skeleton: 5e6, diffusion: 3e6);
        SetHeatFluxes(subject, 1, outSkeleton: 0.0, skeleton: 0.0, diffusion: 0.0);

        var lines = Render(groupResult);
        var (outShare, skelShare, diffShare) = ParseOnly(MeanShareLine, lines);

        Assert.Equal(20.0, outShare, 1);
        Assert.Equal(50.0, skelShare, 1);
        Assert.Equal(30.0, diffShare, 1);

        Assert.Contains(lines, line => line.Contains("non-converged (no surface heat feedback)",
                                                     StringComparison.Ordinal));
    }

    /// <summary>Every point failing must produce a stated failure, not a 0/0 average.</summary>
    [Fact]
    public void MeanShares_AreSuppressedWhenNoPointHadSurfaceHeatFeedback()
    {
        var groupResult = MakeGroup(pressureCount: 2, out var subject);
        SetHeatFluxes(subject, 0, outSkeleton: 0.0, skeleton: 0.0, diffusion: 0.0);
        SetHeatFluxes(subject, 1, outSkeleton: 0.0, skeleton: 0.0, diffusion: 0.0);

        var lines = Render(groupResult);

        Assert.Contains("mean shares: non-converged (no surface heat feedback at any pressure)", lines);
        Assert.DoesNotContain(lines, line => MeanShareLine.IsMatch(line));
    }

    /// <summary>
    /// Characterisation of a gap, not an endorsement of it.
    ///
    /// Every other PDF report gates on <c>BurnRateIsFound</c>, because a failed solve leaves the per-worker
    /// context holding the previous candidate's values rather than zeros. This report gates only on its own
    /// total being positive, so a point whose mixed solve did not converge but whose pocket params still hold
    /// a plausible positive flux renders a complete, confident-looking flame structure — exactly the class of
    /// defect the convergence guard was introduced to remove elsewhere.
    ///
    /// This is reported rather than fixed: the report is product code this change does not own. The
    /// assertion pins today's behaviour so the gap is visible and so a future fix fails here deliberately.
    /// </summary>
    [Fact]
    public void Shares_AreStillPrintedForAPointWhoseMixedSolveDidNotConverge()
    {
        var groupResult = MakeGroup(pressureCount: 1, out var subject);
        SetHeatFluxes(subject, 0, outSkeleton: 1e6, skeleton: 2e6, diffusion: 1e6, burnRateIsFound: false);

        var lines = Render(groupResult);
        var (outShare, skelShare, diffShare) = ParseOnly(ShareLine, lines);

        // Known gap: no NOT CONVERGED marker appears, and the shares read as a result.
        Assert.Equal(25.0, outShare, 1);
        Assert.Equal(50.0, skelShare, 1);
        Assert.Equal(25.0, diffShare, 1);
        Assert.DoesNotContain(lines, line => line.Contains("NOT CONVERGED", StringComparison.Ordinal));
    }
}
