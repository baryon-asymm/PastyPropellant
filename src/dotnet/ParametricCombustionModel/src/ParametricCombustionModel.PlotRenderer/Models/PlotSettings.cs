using OxyPlot;

namespace ParametricCombustionModel.PlotRenderer.Models;

public class PlotSettings
{
    /// <summary>
    /// Gets or sets the title of the plot.
    /// </summary>
    public string Title { get; set; } = "Plot Title";

    /// <summary>
    /// Gets or sets the font size of the plot title.
    /// </summary>
    public double TitleFontSize { get; set; } = 14;

    /// <summary>
    /// Gets or sets the background color of the plot.
    /// </summary>
    public OxyColor BackgroundColor { get; set; } = OxyColors.White;

    /// <summary>
    /// Gets or sets whether the plot legend should be shown.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets the title of the X-axis.
    /// </summary>
    public string XAxisTitle { get; set; } = "X-Axis";

    /// <summary>
    /// Gets or sets the title of the Y-axis.
    /// </summary>
    public string YAxisTitle { get; set; } = "Y-Axis";

    /// <summary>
    /// Gets or sets whether gridlines should be shown.
    /// </summary>
    public bool ShowGridlines { get; set; } = true;

    /// <summary>
    /// Gets or sets the color of the gridlines.
    /// </summary>
    public OxyColor GridlineColor { get; set; } = OxyColors.Gray;

    /// <summary>
    /// Gets or sets the width of the plot image.
    /// </summary>
    public int Width { get; set; } = 8000;

    /// <summary>
    /// Gets or sets the height of the plot image.
    /// </summary>
    public int Height { get; set; } = 8000;

    /// <summary>
    /// Gets or sets the quality of the saved image (relevant for formats like JPEG).
    /// </summary>
    public int Quality { get; set; } = 100;

    /// <summary>
    /// Gets or sets the dots per inch (DPI) for the saved image, affecting its resolution.
    /// </summary>
    public float Dpi { get; set; } = 600.0f;

    /// <summary>
    /// Gets or sets the minimum value of the X-axis.
    /// </summary>
    public double XAxisMinimum { get; set; } = double.NaN;

    /// <summary>
    /// Gets or sets the maximum value of the X-axis.
    /// </summary>
    public double XAxisMaximum { get; set; } = double.NaN;

    /// <summary>
    /// Gets or sets the minimum value of the Y-axis.
    /// </summary>
    public double YAxisMinimum { get; set; } = double.NaN;

    /// <summary>
    /// Gets or sets the maximum value of the Y-axis.
    /// </summary>
    public double YAxisMaximum { get; set; } = double.NaN;
}
