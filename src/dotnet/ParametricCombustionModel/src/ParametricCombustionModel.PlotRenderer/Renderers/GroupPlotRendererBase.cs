using OxyPlot;
using OxyPlot.Series;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.PlotRenderer.Interfaces;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Renderers;

/// <summary>
/// Shared behaviour for renderers that draw a whole <see cref="GroupOptimizationResult"/> onto one set
/// of axes: the fuel-collection walk over the three composition contexts, the colour / marker identity
/// assigned to each fuel, and the series-adding helper.
/// </summary>
public abstract class GroupPlotRendererBase : BasePlotRenderer, IGroupPlotRenderer
{
    /// <summary>
    /// Colour cycle. Assignment order is load-bearing: it is what keeps a fuel the same colour across
    /// the linear and the log-log plot.
    /// </summary>
    protected static readonly OxyColor[] Colors =
    [
        OxyColors.Blue,
        OxyColors.Green,
        OxyColors.Red,
        OxyColors.Violet,
        OxyColors.Orange
    ];

    /// <summary>Marker cycle, advanced in lockstep with <see cref="Colors"/>.</summary>
    protected static readonly MarkerType[] MarkerTypes =
    [
        MarkerType.Circle,
        MarkerType.Triangle,
        MarkerType.Plus,
        MarkerType.Square,
        MarkerType.Diamond
    ];

    /// <summary>
    /// Number of composition groups in the 32-parameter grouped formulation:
    /// 0 = Bas_2 + Bas_3 + Bas_4, 1 = Bas_1, 2 = Bas_0.
    /// </summary>
    private const int CompositionGroupCount = 3;

    /// <summary>
    /// Sub-entries of the Bas_2 composition that are not standalone fuels. They live in the context
    /// matrix because the solver needs them, but they must never appear as their own plot series —
    /// and, just as importantly, they must not consume a slot in the colour / marker cycle.
    /// </summary>
    private static readonly string[] NonPlottablePropellantNames = ["Bas_21", "Bas_22"];

    /// <inheritdoc />
    public abstract void Render(GroupOptimizationResult groupResult, PlotSettings settings);

    /// <summary>
    /// Walks the three composition contexts in index order and, within each, the propellants in matrix
    /// order, skipping the non-plottable sub-entries. Colour and marker advance only on fuels that are
    /// actually kept, so the returned order and identities match what the renderers drew before this
    /// collection step was factored out.
    /// </summary>
    protected static IReadOnlyList<GroupPlotSeriesData> CollectSeriesData(GroupOptimizationResult groupResult)
    {
        var collected = new List<GroupPlotSeriesData>();
        var colorIndex = 0;

        for (var groupIdx = 0; groupIdx < CompositionGroupCount; groupIdx++)
        {
            var compositionContext = groupResult.CompositionContexts[groupIdx];

            for (var i = 0; i < compositionContext.PropellantCount; i++)
            {
                var propellant = compositionContext.ProblemContextMatrix[i, 0].Propellant;
                var propellantName = propellant.Name;

                if (NonPlottablePropellantNames.Contains(propellantName))
                    continue;

                var calculatedDataPoints = new List<DataPoint>();
                var experimentalDataPoints = new List<DataPoint>();

                for (var j = 0; j < compositionContext.PressureCount; j++)
                {
                    var context = compositionContext.ProblemContextMatrix[i, j];

                    calculatedDataPoints.Add(new DataPoint(
                        context.Pressure.Megapascals,
                        context.MixedCombustionParams.BurnRate.MillimetersPerSecond));

                    experimentalDataPoints.Add(new DataPoint(
                        context.Pressure.Megapascals,
                        compositionContext.ExperimentalBurnRates[i, j].MillimetersPerSecond));
                }

                collected.Add(new GroupPlotSeriesData(
                    propellantName,
                    propellant,
                    Colors[colorIndex % Colors.Length],
                    MarkerTypes[colorIndex % MarkerTypes.Length],
                    calculatedDataPoints,
                    experimentalDataPoints));

                colorIndex++;
            }
        }

        return collected;
    }

    /// <summary>
    /// Adds one burn-rate curve to the plot model.
    /// </summary>
    protected static void AddLineSeries(
        PlotModel plotModel,
        IEnumerable<DataPoint> data,
        string seriesTitle,
        OxyColor color,
        MarkerType markerType,
        bool renderInLegend = true,
        LineStyle lineStyle = LineStyle.Solid)
    {
        plotModel.Series.Add(new LineSeries
        {
            Title = seriesTitle,
            ItemsSource = data,
            Color = color,
            LineStyle = lineStyle,
            RenderInLegend = renderInLegend,
            MarkerType = markerType,
            MarkerSize = 4
        });
    }
}
