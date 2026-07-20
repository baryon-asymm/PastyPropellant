using OxyPlot;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.PlotRenderer.Models;

/// <summary>
/// One plottable fuel harvested from a grouped optimisation result: its calculated and experimental
/// burn-rate curves plus the colour / marker identity assigned to it. The identity is assigned once,
/// during collection, so that a fuel looks the same on every group plot.
/// </summary>
/// <param name="PropellantName">Fuel name, used to build the series titles.</param>
/// <param name="Propellant">The source propellant, carried so renderers can reach its confidence intervals.</param>
/// <param name="Color">Colour assigned to both of this fuel's series.</param>
/// <param name="MarkerType">Marker assigned to both of this fuel's series.</param>
/// <param name="CalculatedPoints">Model burn rate (mm/s) against pressure (MPa).</param>
/// <param name="ExperimentalPoints">Measured burn rate (mm/s) against pressure (MPa).</param>
public sealed record GroupPlotSeriesData(
    string PropellantName,
    Propellant Propellant,
    OxyColor Color,
    MarkerType MarkerType,
    IReadOnlyList<DataPoint> CalculatedPoints,
    IReadOnlyList<DataPoint> ExperimentalPoints);
