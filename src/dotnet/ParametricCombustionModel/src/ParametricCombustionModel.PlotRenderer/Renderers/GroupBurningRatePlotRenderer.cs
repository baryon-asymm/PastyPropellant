using OxyPlot;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.PlotRenderer.Drawers;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Renderers;

/// <summary>
/// Renders a unified burning rate plot combining all propellants from all three composition groups.
/// This renderer creates a single comprehensive plot showing calculated vs experimental burning rates
/// for all propellants (Bas_0, Bas_1, Bas_2, Bas_3, Bas_4) optimized simultaneously.
/// </summary>
public class GroupBurningRatePlotRenderer : GroupPlotRendererBase
{
    /// <summary>
    /// Renders a unified burning rate plot for group optimization results.
    /// Combines all propellants from all three composition groups into a single plot.
    /// </summary>
    /// <param name="groupResult">The group optimization result containing contexts for all three groups.</param>
    /// <param name="settings">The plot rendering settings.</param>
    public override void Render(
        GroupOptimizationResult groupResult,
        PlotSettings settings)
    {
        var plotModel = CreatePlotModel(settings);

        foreach (var fuel in CollectSeriesData(groupResult))
        {
            // Calculated burning rate series (solid line)
            AddLineSeries(
                plotModel,
                fuel.CalculatedPoints,
                $"{fuel.PropellantName} (Calculated)",
                fuel.Color,
                fuel.MarkerType,
                renderInLegend: true);

            // Experimental burning rate series (dashed line)
            AddLineSeries(
                plotModel,
                fuel.ExperimentalPoints,
                $"{fuel.PropellantName} (Experimental)",
                fuel.Color,
                fuel.MarkerType,
                renderInLegend: true,
                lineStyle: LineStyle.Dash);

            // Confidence intervals, when the propellant carries them
            if (fuel.Propellant.ConfidenceIntervals == null)
                continue;

            foreach (var confidenceInterval in fuel.Propellant.ConfidenceIntervals)
            {
                var confidenceIntervalDrawer = new ConfidenceIntervalDrawer(
                    confidenceInterval.XValue,
                    confidenceInterval.YValue,
                    confidenceInterval.SizeOfConfidenceInterval,
                    0.1);

                confidenceIntervalDrawer.Draw(plotModel, fuel.Color);
            }
        }

        SavePlotToFile(plotModel, "group_burning_rate_plot.jpg", settings);
    }
}
