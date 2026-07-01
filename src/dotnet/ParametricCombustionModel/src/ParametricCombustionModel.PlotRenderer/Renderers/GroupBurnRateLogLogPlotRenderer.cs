using OxyPlot;
using OxyPlot.Series;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Renderers;

/// <summary>
/// Renders the group burning rate the same way <see cref="GroupBurningRatePlotRenderer"/> does, but on
/// base-10 logarithmic axes (log r vs log p). A Vieille power law U = A·p^v is a straight line in log-log
/// space (log U = log A + v·log p), so this plot is the visual counterpart of the numeric A/v fit printed
/// by the report: the experimental slope is v_exp and the calculated slope is v_calc, and a fuel whose
/// model curve is steeper/shallower than its experimental points has a pressure-exponent error (dv) that is
/// hard to see on the linear plot but obvious here.
///
/// Confidence-interval whiskers are intentionally omitted: the drawer used on the linear plot lays them out
/// with an absolute (linear) size, which is meaningless on a logarithmic axis.
/// </summary>
public class GroupBurnRateLogLogPlotRenderer : BasePlotRenderer
{
    // Same colour / marker assignment order as GroupBurningRatePlotRenderer so a fuel keeps its identity
    // across the linear and log-log plots.
    private static readonly OxyColor[] Colors =
    [
        OxyColors.Blue,
        OxyColors.Green,
        OxyColors.Red,
        OxyColors.Violet,
        OxyColors.Orange
    ];

    private static readonly MarkerType[] MarkerTypes =
    [
        MarkerType.Circle,
        MarkerType.Triangle,
        MarkerType.Plus,
        MarkerType.Square,
        MarkerType.Diamond
    ];

    /// <summary>
    /// Not used for group optimization; use the <see cref="Render(GroupOptimizationResult, PlotSettings)"/>
    /// overload instead.
    /// </summary>
    public override void Render(OptimizationResult result, PlotSettings settings)
    {
        throw new NotSupportedException("Use the Render(GroupOptimizationResult, PlotSettings) overload instead.");
    }

    /// <summary>
    /// Renders the unified log-log burning rate plot for a group optimization result.
    /// </summary>
    public void Render(
        GroupOptimizationResult groupResult,
        PlotSettings settings)
    {
        // Force logarithmic axes regardless of how the caller configured the settings; a linear
        // caller-supplied range would otherwise silently produce a linear plot from this renderer.
        settings.UseLogarithmicAxes = true;

        var plotModel = CreatePlotModel(settings);

        var colorIndex = 0;
        for (var groupIdx = 0; groupIdx < 3; groupIdx++)
        {
            var compositionContext = groupResult.CompositionContexts[groupIdx];

            for (var i = 0; i < compositionContext.PropellantCount; i++)
            {
                var propellantName = compositionContext.ProblemContextMatrix[i, 0].Propellant.Name;
                if (propellantName.Equals("Bas_21") || propellantName.Equals("Bas_22"))
                    continue;

                var color = Colors[colorIndex % Colors.Length];
                var markerType = MarkerTypes[colorIndex % MarkerTypes.Length];

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

                AddLineSeries(
                    plotModel,
                    calculatedDataPoints,
                    $"{propellantName} (Calculated)",
                    color,
                    markerType,
                    lineStyle: LineStyle.Solid);

                AddLineSeries(
                    plotModel,
                    experimentalDataPoints,
                    $"{propellantName} (Experimental)",
                    color,
                    markerType,
                    lineStyle: LineStyle.Dash);

                colorIndex++;
            }
        }

        SavePlotToFile(plotModel, "group_burning_rate_loglog_plot.jpg", settings);
    }

    private static void AddLineSeries(
        PlotModel plotModel,
        IEnumerable<DataPoint> data,
        string seriesTitle,
        OxyColor color,
        MarkerType markerType,
        LineStyle lineStyle)
    {
        plotModel.Series.Add(new LineSeries
        {
            Title = seriesTitle,
            ItemsSource = data,
            Color = color,
            LineStyle = lineStyle,
            RenderInLegend = true,
            MarkerType = markerType,
            MarkerSize = 4
        });
    }
}
