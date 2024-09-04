using OxyPlot;

namespace ParametricCombustionModel.PlotRenderer.Extensions;

public static class OxyColorExtensions
{
    /// <summary>
    /// Darkens the color by reducing its brightness.
    /// </summary>
    /// <param name="color">The original color.</param>
    /// <param name="factor">The factor by which to darken the color (0 to 1).</param>
    /// <returns>The darkened color.</returns>
    public static OxyColor Darken(this OxyColor color, double factor)
    {
        // Ensure the factor is between 0 and 1
        factor = Math.Max(0, Math.Min(factor, 1));

        // Reduce RGB values
        var r = (byte)(color.R * (1 - factor));
        var g = (byte)(color.G * (1 - factor));
        var b = (byte)(color.B * (1 - factor));
        
        return OxyColor.FromRgb(r, g, b);
    }
}
