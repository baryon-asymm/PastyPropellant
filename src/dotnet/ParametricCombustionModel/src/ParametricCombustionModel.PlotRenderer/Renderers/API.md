# API.md — Renderers

Пространство имён `ParametricCombustionModel.PlotRenderer.Renderers`.

## Output base ✅

```csharp
public abstract class BasePlotRenderer : IPlotRenderer
{
    protected PlotModel CreatePlotModel(PlotSettings settings);
    public void SavePlotToFile(PlotModel plotModel, string filePath, PlotSettings settings);
    public byte[] GetPlotAsByteArray(PlotModel plotModel, PlotSettings settings);
    public Stream GetPlotAsStream(PlotModel plotModel, PlotSettings settings);
}
```

## Group base ✅

```csharp
public abstract class GroupPlotRendererBase : BasePlotRenderer, IGroupPlotRenderer
{
    protected static readonly OxyColor[] Colors;
    protected static readonly MarkerType[] MarkerTypes;

    public abstract void Render(GroupOptimizationResult groupResult, PlotSettings settings);

    // Отбрасывает Bas_21 / Bas_22, не расходуя на них цветовой слот.
    protected static IReadOnlyList<GroupPlotSeriesData> CollectSeriesData(GroupOptimizationResult groupResult);
    protected static void AddLineSeries(PlotModel plotModel, GroupPlotSeriesData series);
}
```

## Concrete renderers ✅

```csharp
// -> group_burning_rate_plot.jpg
public class GroupBurningRatePlotRenderer : GroupPlotRendererBase
{
    public override void Render(GroupOptimizationResult groupResult, PlotSettings settings);
}

// -> group_burning_rate_loglog_plot.jpg; степенной закон здесь — прямая.
public class GroupBurnRateLogLogPlotRenderer : GroupPlotRendererBase
{
    public override void Render(GroupOptimizationResult groupResult, PlotSettings settings);
}
```
