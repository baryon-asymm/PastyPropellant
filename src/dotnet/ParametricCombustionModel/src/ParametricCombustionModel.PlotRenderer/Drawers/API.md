# API.md — Drawers

Пространство имён `ParametricCombustionModel.PlotRenderer.Drawers`.

## Confidence interval ✅

```csharp
public class ConfidenceIntervalDrawer
{
    public double XValue { get; init; }
    public double UpperBoundConfidenceInterval { get; init; }
    public double LowerBoundConfidenceInterval { get; init; }
    public double SizeOfWhiskers { get; init; }        // в единицах оси X

    public ConfidenceIntervalDrawer(
        double xValue,
        double upperBoundConfidenceInterval,
        double lowerBoundConfidenceInterval,
        double sizeOfWhiskers);

    public void Draw(PlotModel plotModel);
}
```
