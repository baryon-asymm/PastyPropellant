# API.md — Interfaces

Пространство имён `ParametricCombustionModel.PlotRenderer.Interfaces`.

## Output contract ✅

```csharp
public interface IPlotRenderer
{
    void SavePlotToFile(PlotModel plotModel, string filePath, PlotSettings settings);
    byte[] GetPlotAsByteArray(PlotModel plotModel, PlotSettings settings);
    Stream GetPlotAsStream(PlotModel plotModel, PlotSettings settings);
}
```

## Data contracts ✅

```csharp
public interface IGroupPlotRenderer : IPlotRenderer
{
    void Render(GroupOptimizationResult groupResult, PlotSettings settings);
}

// Реализаций в дереве нет.
public interface ISingleResultPlotRenderer : IPlotRenderer
{
    void Render(OptimizationResult result, PlotSettings settings);
}
```
