# API.md — Optimizers

Пространство имён `ParametricCombustionModel.Optimization.Optimizers`.

## Group optimizer ✅

```csharp
public class GroupDifferentialEvolutionOptimizer : IFitnessFunctionEvaluator
{
    public IFitnessFunctionVisitor FitnessFunctionSolver { get; init; }

    // compositionProblems: [воркер, группа]; группа 0 = Bas_2+Bas_3+Bas_4, 1 = Bas_1, 2 = Bas_0.
    public GroupDifferentialEvolutionOptimizer(
        DifferentialEvolutionSettings settings,
        OptimizationProblemByDoubles[,] compositionProblems,
        OptimizationProblemByUnits[] finalContextsByUnits);

    public Task<OperationResult<GroupOptimizationResult>> RunAsync();

    // Прямой расчёт одного вектора без поиска; тот же путь, что финальная оценка.
    public GroupOptimizationResult EvaluateVector(double[] genes);

    public double Evaluate(ReadOnlySpan<double> genes);                        // воркер 0
    public double Evaluate(int workerIndex, ReadOnlySpan<double> genes);       // mean(3) + штрафы
}
```

## Not part of the contract

Наружу не виден: `DifferentialEvolutionStrategyApplier` объявлен `internal`,
применяет выбранную стратегию к построителю библиотеки и не входит в контракт
узла.
