using OxyPlot;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.ReportMaking.Models;

namespace ParametricCombustionModel.PlotRenderer.Interfaces;

public interface IPlotRenderer
{
    /// <summary>
    /// Renders the plot based on the provided report and settings.
    /// </summary>
    /// <param name="report">The report containing the data for the plot.</param>
    /// <param name="settings">Settings for the plot such as titles, axis labels, and other parameters.</param>
    void Render(Report report, PlotSettings settings);

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
