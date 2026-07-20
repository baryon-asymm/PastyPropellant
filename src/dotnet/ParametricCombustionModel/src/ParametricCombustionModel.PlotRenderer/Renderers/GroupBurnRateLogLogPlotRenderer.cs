using OxyPlot;
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
///
/// The colour / marker assignment comes from the shared collection walk in <see cref="GroupPlotRendererBase"/>,
/// which is what keeps a fuel's identity consistent with the linear plot.
/// </summary>
public class GroupBurnRateLogLogPlotRenderer : GroupPlotRendererBase
{
    /// <summary>
    /// Renders the unified log-log burning rate plot for a group optimization result.
    /// </summary>
    public override void Render(
        GroupOptimizationResult groupResult,
        PlotSettings settings)
    {
        // Force logarithmic axes regardless of how the caller configured the settings; a linear
        // caller-supplied range would otherwise silently produce a linear plot from this renderer.
        settings.UseLogarithmicAxes = true;

        var plotModel = CreatePlotModel(settings);

        foreach (var fuel in CollectSeriesData(groupResult))
        {
            AddLineSeries(
                plotModel,
                fuel.CalculatedPoints,
                $"{fuel.PropellantName} (Calculated)",
                fuel.Color,
                fuel.MarkerType,
                lineStyle: LineStyle.Solid);

            AddLineSeries(
                plotModel,
                fuel.ExperimentalPoints,
                $"{fuel.PropellantName} (Experimental)",
                fuel.Color,
                fuel.MarkerType,
                lineStyle: LineStyle.Dash);
        }

        SavePlotToFile(plotModel, "group_burning_rate_loglog_plot.jpg", settings);
    }
}
