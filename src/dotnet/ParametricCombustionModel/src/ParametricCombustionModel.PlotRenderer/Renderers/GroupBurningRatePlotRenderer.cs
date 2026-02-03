using OxyPlot;
using OxyPlot.Series;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.PlotRenderer.Drawers;
using ParametricCombustionModel.PlotRenderer.Extensions;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Renderers;

/// <summary>
/// Renders a unified burning rate plot combining all propellants from all three composition groups.
/// This renderer creates a single comprehensive plot showing calculated vs experimental burning rates
/// for all propellants (Bas_0, Bas_1, Bas_2, Bas_3, Bas_4) optimized simultaneously.
/// </summary>
public class GroupBurningRatePlotRenderer : BasePlotRenderer
{
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
    /// Implements the abstract Render method from BasePlotRenderer.
    /// This method is not used for group optimization; use the overloaded Render method instead.
    /// </summary>
    public override void Render(OptimizationResult result, PlotSettings settings)
    {
        throw new NotSupportedException("Use the Render(GroupOptimizationResult, PlotSettings) overload instead.");
    }

    /// <summary>
    /// Renders a unified burning rate plot for group optimization results.
    /// Combines all propellants from all three composition groups into a single plot.
    /// </summary>
    /// <param name="groupResult">The group optimization result containing contexts for all three groups.</param>
    /// <param name="settings">The plot rendering settings.</param>
    public void Render(
        GroupOptimizationResult groupResult,
        PlotSettings settings)
    {
        var plotModel = CreatePlotModel(settings);

        // Add series to the plot - iterate through all composition groups
        int colorIndex = 0;
        
        for (int groupIdx = 0; groupIdx < 3; groupIdx++)
        {
            var compositionContext = groupResult.CompositionContexts[groupIdx];

            // Iterate through all propellants in this group
            for (int i = 0; i < compositionContext.PropellantCount; i++)
            {
                // Skip Bas_21 and Bas_22 (if present)
                var propellantName = compositionContext.ProblemContextMatrix[i, 0].Propellant.Name;
                if (propellantName.Equals("Bas_21") || propellantName.Equals("Bas_22"))
                {
                    continue;
                }

                var color = Colors[colorIndex % Colors.Length];
                var markerType = MarkerTypes[colorIndex % MarkerTypes.Length];

                var calculatedDataPoints = new List<DataPoint>();
                var experimentalDataPoints = new List<DataPoint>();

                // Collect data points for all pressure points
                for (int j = 0; j < compositionContext.PressureCount; j++)
                {
                    var context = compositionContext.ProblemContextMatrix[i, j];

                    calculatedDataPoints.Add(new DataPoint(
                        context.Pressure.Megapascals,
                        context.MixedCombustionParams.BurnRate.MillimetersPerSecond));

                    experimentalDataPoints.Add(new DataPoint(
                        context.Pressure.Megapascals,
                        compositionContext.ExperimentalBurnRates[i, j].MillimetersPerSecond));
                }

                // Add calculated burning rate series (solid line)
                AddLineSeries(
                    plotModel,
                    calculatedDataPoints,
                    $"{propellantName} (Calculated)",
                    color,
                    markerType,
                    renderInLegend: true);

                // Add experimental burning rate series (dashed line)
                AddLineSeries(
                    plotModel,
                    experimentalDataPoints,
                    $"{propellantName} (Experimental)",
                    color,
                    markerType,
                    renderInLegend: true,
                    lineStyle: LineStyle.Dash);

                // Add confidence intervals if available
                var propellant = compositionContext.ProblemContextMatrix[i, 0].Propellant;
                if (propellant.ConfidenceIntervals != null)
                {
                    foreach (var confidenceInterval in propellant.ConfidenceIntervals)
                    {
                        var confidenceIntervalDrawer = new ConfidenceIntervalDrawer(
                            confidenceInterval.XValue,
                            confidenceInterval.YValue,
                            confidenceInterval.SizeOfConfidenceInterval,
                            0.1);

                        confidenceIntervalDrawer.Draw(plotModel, color);
                    }
                }

                colorIndex++;
            }
        }

        // Save the plot
        SavePlotToFile(plotModel, "group_burning_rate_plot.jpg", settings);
    }

    /// <summary>
    /// Adds a line series to the plot model.
    /// </summary>
    /// <param name="plotModel">The plot model to add the series to.</param>
    /// <param name="data">The data points for the series.</param>
    /// <param name="seriesTitle">The title of the series.</param>
    /// <param name="color">The color of the line.</param>
    /// <param name="markerType">The type of marker to use.</param>
    /// <param name="renderInLegend">Whether to render in legend.</param>
    /// <param name="lineStyle">The style of the line.</param>
    protected void AddLineSeries(
        PlotModel plotModel,
        IEnumerable<DataPoint> data,
        string seriesTitle,
        OxyColor color,
        MarkerType markerType,
        bool renderInLegend = true,
        LineStyle lineStyle = LineStyle.Solid)
    {
        var series = new LineSeries
        {
            Title = seriesTitle,
            ItemsSource = data,
            Color = color,
            LineStyle = lineStyle,
            RenderInLegend = renderInLegend,
            MarkerType = markerType,
            MarkerSize = 4
        };

        plotModel.Series.Add(series);
    }
}

