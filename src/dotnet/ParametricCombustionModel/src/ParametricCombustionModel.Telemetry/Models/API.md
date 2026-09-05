# API.md — Models (Telemetry)

⚠ Пространство имён — `ParametricCombustionModel.Optimization.Models`, а не
`ParametricCombustionModel.Telemetry.Models`. Причина — в [BOOT.md](./BOOT.md).

## Measuring problem context ✅

```csharp
public class MeasureOptimizationProblemByDoubles : OptimizationProblemByDoubles
{
    public MeasureOptimizationProblemByDoubles(
        PerformanceMeter performanceMeter,
        int processorId,
        ProblemContextByDoubles[,] problemContextMatrix,
        ISolverVisitor solver,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators);

    public override void Accept(in CombustionSolverParamsByDoubles solverParams, IFitnessFunctionVisitor fitnessFunction);
    public override void Accept(in CombustionSolverParamsByUnits solverParamsByUnits, IFitnessFunctionVisitor fitnessFunction);
}
```

Первая перегрузка оборачивает вызов кадром измерения. Вторая всегда бросает
`NotSupportedException`: тир `ByUnits` через измеряющий контекст не ходит.
`processorId` задаёт имя и путь прибора, чем и обеспечивается «один
измеритель на воркер».
