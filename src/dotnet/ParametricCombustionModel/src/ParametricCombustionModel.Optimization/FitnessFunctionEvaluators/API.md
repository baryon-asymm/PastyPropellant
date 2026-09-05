# API.md — FitnessFunctionEvaluators

Пространство имён
`ParametricCombustionModel.Optimization.FitnessFunctionEvaluators`.
Результат пишется в переданную задачу, а не возвращается.

## Fitness ✅

```csharp
// mean по топливам от sqrt(mean по давлениям от ((r_calc - r_exp) / r_exp)^2);
// double.MaxValue, если хотя бы одна точка не сошлась.
public class FitnessFunctionEvaluator : IFitnessFunctionVisitor
{
    public virtual void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, OptimizationProblemByUnits context);
    public virtual void Visit(in CombustionSolverParamsByDoubles solverParams, OptimizationProblemByDoubles context);
}
```

## Fitness with penalties ✅

```csharp
// Сбрасывает штрафы, считает целевую функцию, затем — при конечном её значении —
// накапливает штраф каждого оценщика по всем топливам и давлениям.
public class PenaltyFitnessFunctionEvaluator : FitnessFunctionEvaluator
{
    public override void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, OptimizationProblemByUnits context);
    public override void Visit(in CombustionSolverParamsByDoubles solverParams, OptimizationProblemByDoubles context);
}
```
