using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Interfaces;

/// <summary>
/// A renderer that plots a single-composition <see cref="OptimizationResult"/>.
/// </summary>
public interface ISingleResultPlotRenderer : IPlotRenderer
{
    /// <summary>
    /// Renders the plot based on the provided result and settings.
    /// </summary>
    /// <param name="result">The result containing the data for the plot.</param>
    /// <param name="settings">Settings for the plot such as titles, axis labels, and other parameters.</param>
    void Render(OptimizationResult result, PlotSettings settings);
}
