using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Interfaces;

/// <summary>
/// A renderer that plots a <see cref="GroupOptimizationResult"/> — the three composition contexts of a
/// grouped run drawn onto one set of axes.
/// </summary>
public interface IGroupPlotRenderer : IPlotRenderer
{
    /// <summary>
    /// Renders the plot based on the provided group result and settings.
    /// </summary>
    /// <param name="groupResult">The group optimization result containing contexts for all three groups.</param>
    /// <param name="settings">Settings for the plot such as titles, axis labels, and other parameters.</param>
    void Render(GroupOptimizationResult groupResult, PlotSettings settings);
}
