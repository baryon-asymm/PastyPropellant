using OxyPlot;
using ParametricCombustionModel.PlotRenderer.Models;

namespace ParametricCombustionModel.PlotRenderer.Interfaces;

/// <summary>
/// Export surface shared by every renderer, regardless of the shape of the result it plots.
/// The <c>Render</c> entry point deliberately lives on the result-specific interfaces
/// (<see cref="ISingleResultPlotRenderer"/> / <see cref="IGroupPlotRenderer"/>) so that a renderer
/// never has to declare — and stub out — a Render overload for a result type it cannot plot.
/// </summary>
public interface IPlotRenderer
{
    /// <summary>
    /// Saves the rendered plot to a file.
    /// </summary>
    /// <param name="plotModel">The plot model to save.</param>
    /// <param name="filePath">The file path where the plot will be saved.</param>
    /// <param name="settings">Settings for the plot such as width and height.</param>
    void SavePlotToFile(PlotModel plotModel, string filePath, PlotSettings settings);

    /// <summary>
    /// Returns the rendered plot as a byte array.
    /// </summary>
    /// <param name="plotModel">The plot model to convert to an image.</param>
    /// <param name="settings">Settings for the plot such as width and height.</param>
    /// <returns>A byte array representing the plot image.</returns>
    byte[] GetPlotAsByteArray(PlotModel plotModel, PlotSettings settings);

    /// <summary>
    /// Returns the rendered plot as a stream.
    /// </summary>
    /// <param name="plotModel">The plot model to convert to an image.</param>
    /// <param name="settings">Settings for the plot such as width and height.</param>
    /// <returns>A stream representing the plot image.</returns>
    Stream GetPlotAsStream(PlotModel plotModel, PlotSettings settings);
}
