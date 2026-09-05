# API.md — Interfaces

Пространство имён `ParametricCombustionModel.Optimization.Interfaces`.

## Fitness visitor contract ✅

```csharp
public interface IFitnessFunctionVisitor
{
    void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, OptimizationProblemByUnits context);
    void Visit(in CombustionSolverParamsByDoubles solverParams, OptimizationProblemByDoubles context);
}

public interface IOptimizationVisitable
{
    void Accept(in CombustionSolverParamsByUnits solverParamsByUnits, IFitnessFunctionVisitor fitnessFunction);
    void Accept(in CombustionSolverParamsByDoubles solverParams, IFitnessFunctionVisitor fitnessFunction);
}
```
