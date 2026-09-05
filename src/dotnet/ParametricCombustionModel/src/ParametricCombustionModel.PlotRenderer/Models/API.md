# API.md — Models

Пространство имён `ParametricCombustionModel.PlotRenderer.Models`.

## Plot settings ✅

```csharp
public class PlotSettings
{
    public string Title { get; set; }                  // "Plot Title"
    public double TitleFontSize { get; set; }          // 14
    public OxyColor BackgroundColor { get; set; }      // White
    public bool ShowLegend { get; set; }               // true
    public string XAxisTitle { get; set; }             // "X-Axis"
    public string YAxisTitle { get; set; }             // "Y-Axis"
    public bool ShowGridlines { get; set; }            // true
    public bool UseLogarithmicAxes { get; set; }       // false
    public OxyColor GridlineColor { get; set; }        // Gray
    public int Width { get; set; }                     // 8000
    public int Height { get; set; }                    // 8000
    public int Quality { get; set; }                   // 100
    public float Dpi { get; set; }                     // 600
    public double XAxisMinimum { get; set; }           // NaN = авто
    public double XAxisMaximum { get; set; }           // NaN = авто
    public double YAxisMinimum { get; set; }           // NaN = авто
    public double YAxisMaximum { get; set; }           // NaN = авто
}
```

## One curve of a group plot ✅

```csharp
public sealed record GroupPlotSeriesData(
    string PropellantName,
    Propellant Propellant,
    OxyColor Color,
    MarkerType MarkerType,
    IReadOnlyList<DataPoint> CalculatedPoints,
    IReadOnlyList<DataPoint> ExperimentalPoints);
```
