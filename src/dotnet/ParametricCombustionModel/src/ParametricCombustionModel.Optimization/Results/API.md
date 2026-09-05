# API.md — Results

Пространство имён `ParametricCombustionModel.Optimization.Results`.

## Group result ✅

```csharp
public class GroupOptimizationResult
{
    // Индексация: 0 = Bas_2+Bas_3+Bas_4, 1 = Bas_1, 2 = Bas_0.
    public OptimizationProblemByUnits[] CompositionContexts { get; init; }
    public double[] LowerBound { get; init; }
    public double[] UpperBound { get; init; }
    public double[] BestParams { get; init; }     // ровно 32 элемента

    public double AggregatedFitness { get; set; }         // среднее трёх
    public double TotalAggregatedPenalty { get; set; }    // сумма трёх
    public double[] IndividualFitnesses { get; set; }
    public double[] IndividualPenalties { get; set; }

    public GroupOptimizationResult(
        OptimizationProblemByUnits[] compositionContexts,
        double[] lowerBound,
        double[] upperBound,
        double[] bestParams);
}
```

## Adapter to the single-composition result ✅

```csharp
public static class GroupOptimizationResultAdapter
{
    // Склеивает три матрицы контекстов в одну 5 x давления; границы отдаёт ПУСТЫМИ.
    public static OptimizationResult ToOptimizationResult(this GroupOptimizationResult groupResult);
}
```
