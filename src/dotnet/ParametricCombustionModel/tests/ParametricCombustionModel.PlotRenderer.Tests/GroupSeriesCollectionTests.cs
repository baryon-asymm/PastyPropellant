using OxyPlot;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;

namespace ParametricCombustionModel.PlotRenderer.Tests;

/// <summary>
/// Coverage for the data-selection step in <see cref="GroupPlotRendererBase"/>: which fuels become plot
/// series, and what colour and marker each one is given.
///
/// Bas_21 and Bas_22 are sub-entries of the Bas_2 composition rather than standalone fuels. They sit in the
/// context matrix because the solver needs them, and they must be dropped before plotting. The subtle half of
/// that rule is that they must not consume a slot in the colour / marker cycle either: skipping the series but
/// still advancing the index would silently recolour every fuel after them, and because the colour identity is
/// what ties a fuel together across the linear and log-log plots, the two plots would disagree.
/// </summary>
public class GroupSeriesCollectionTests
{
    /// <summary>Exposes the protected collection step, which is otherwise reachable only from a renderer.</summary>
    private sealed class TestableGroupRenderer : GroupPlotRendererBase
    {
        public override void Render(Optimization.Results.GroupOptimizationResult groupResult, PlotSettings settings) =>
            throw new NotSupportedException("Not exercised: these tests cover collection, not drawing.");

        public static IReadOnlyList<GroupPlotSeriesData> Collect(
            Optimization.Results.GroupOptimizationResult groupResult) => CollectSeriesData(groupResult);
    }

    /// <summary>
    /// A grouped result whose three compositions carry the named fuels, at two pressures each, with a
    /// converged burn rate everywhere so the point lists are populated.
    /// </summary>
    private static Optimization.Results.GroupOptimizationResult MakeGroup(
        IReadOnlyList<string> composition0,
        IReadOnlyList<string> composition1,
        IReadOnlyList<string> composition2) =>
        new(
            [MakeComposition(composition0), MakeComposition(composition1), MakeComposition(composition2)],
            new double[32],
            Enumerable.Repeat(1.0, 32).ToArray(),
            new double[32]);

    private static OptimizationProblemByUnits MakeComposition(IReadOnlyList<string> fuelNames) =>
        PlotRendererFixture.MakeProblem(fuelNames, [1e6, 1e7]);

    /// <summary>The sub-entries are dropped; every other fuel survives, in matrix then composition order.</summary>
    [Fact]
    public void CollectSeriesData_DropsTheBas2SubEntries()
    {
        var groupResult = MakeGroup(["Bas_2", "Bas_21", "Bas_22", "Bas_3", "Bas_4"], ["Bas_1"], ["Bas_0"]);

        var collected = TestableGroupRenderer.Collect(groupResult);

        Assert.Equal(["Bas_2", "Bas_3", "Bas_4", "Bas_1", "Bas_0"],
                     collected.Select(series => series.PropellantName));
    }

    /// <summary>
    /// The load-bearing half of the skip: the colour cycle must advance only on kept fuels. Bas_3 follows the
    /// two sub-entries in the matrix, so if their slots were consumed it would be assigned the fourth colour
    /// (Violet) instead of the second (Green).
    /// </summary>
    [Fact]
    public void CollectSeriesData_SkippedSubEntriesDoNotConsumeAColourSlot()
    {
        var withSubEntries = TestableGroupRenderer.Collect(
            MakeGroup(["Bas_2", "Bas_21", "Bas_22", "Bas_3", "Bas_4"], ["Bas_1"], ["Bas_0"]));

        var withoutSubEntries = TestableGroupRenderer.Collect(
            MakeGroup(["Bas_2", "Bas_3", "Bas_4"], ["Bas_1"], ["Bas_0"]));

        Assert.Equal(withoutSubEntries.Select(s => (s.PropellantName, s.Color, s.MarkerType)),
                     withSubEntries.Select(s => (s.PropellantName, s.Color, s.MarkerType)));

        // Pinned explicitly so the intent survives even if both sides regress together.
        Assert.Equal(OxyColors.Blue, withSubEntries[0].Color);
        Assert.Equal(OxyColors.Green, withSubEntries[1].Color);
        Assert.Equal("Bas_3", withSubEntries[1].PropellantName);
    }

    /// <summary>Colour and marker advance in lockstep, which is what keeps a fuel identifiable in monochrome.</summary>
    [Fact]
    public void CollectSeriesData_AssignsColourAndMarkerInLockstep()
    {
        var collected = TestableGroupRenderer.Collect(
            MakeGroup(["Bas_2", "Bas_21", "Bas_3"], ["Bas_1"], ["Bas_0"]));

        Assert.Equal(
            [(OxyColors.Blue, MarkerType.Circle),
             (OxyColors.Green, MarkerType.Triangle),
             (OxyColors.Red, MarkerType.Plus),
             (OxyColors.Violet, MarkerType.Square)],
            collected.Select(series => (series.Color, series.MarkerType)));
    }

    /// <summary>
    /// A sub-entry appearing anywhere in the matrix must be skipped — the rule is on the name, not on a
    /// position, so a data file that orders the sub-entries first must not shift the colour assignment.
    /// </summary>
    [Fact]
    public void CollectSeriesData_DropsSubEntriesRegardlessOfTheirPositionInTheMatrix()
    {
        var subEntriesFirst = TestableGroupRenderer.Collect(
            MakeGroup(["Bas_21", "Bas_22", "Bas_2"], ["Bas_1"], ["Bas_0"]));

        Assert.Equal(["Bas_2", "Bas_1", "Bas_0"], subEntriesFirst.Select(series => series.PropellantName));
        Assert.Equal(OxyColors.Blue, subEntriesFirst[0].Color);
    }

    /// <summary>A composition consisting only of sub-entries contributes nothing rather than an empty series.</summary>
    [Fact]
    public void CollectSeriesData_ACompositionOfOnlySubEntriesContributesNoSeries()
    {
        var collected = TestableGroupRenderer.Collect(
            MakeGroup(["Bas_21", "Bas_22"], ["Bas_1"], ["Bas_0"]));

        Assert.Equal(["Bas_1", "Bas_0"], collected.Select(series => series.PropellantName));
    }

    /// <summary>
    /// The skip is exact, not a prefix match: Bas_2 shares a prefix with both sub-entry names, and dropping
    /// it would empty the plot of the composition the whole group is named for.
    /// </summary>
    [Fact]
    public void CollectSeriesData_KeepsBas2Itself()
    {
        var collected = TestableGroupRenderer.Collect(MakeGroup(["Bas_2"], ["Bas_1"], ["Bas_0"]));

        Assert.Contains("Bas_2", collected.Select(series => series.PropellantName));
    }

    /// <summary>Each kept fuel carries one point per pressure on both curves.</summary>
    [Fact]
    public void CollectSeriesData_EmitsOnePointPerPressureOnBothCurves()
    {
        var collected = TestableGroupRenderer.Collect(
            MakeGroup(["Bas_2", "Bas_21"], ["Bas_1"], ["Bas_0"]));

        Assert.All(collected, series =>
        {
            Assert.Equal(2, series.CalculatedPoints.Count);
            Assert.Equal(2, series.ExperimentalPoints.Count);
        });

        // Pressures are carried in MPa, in matrix order.
        Assert.Equal([1.0, 10.0], collected[0].CalculatedPoints.Select(point => point.X));
    }
}
