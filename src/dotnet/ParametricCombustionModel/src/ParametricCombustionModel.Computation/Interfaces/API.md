# API.md — Interfaces

Пространство имён `ParametricCombustionModel.Computation.Interfaces`.

## Visitor contract ✅

```csharp
public interface IComputationVisitable
{
    void Accept(in CombustionSolverParamsByUnits solverParamsByUnits, ISolverVisitor solver);
    void Accept(in CombustionSolverParamsByDoubles solverParams, ISolverVisitor solver);
}

public interface ISolverVisitor
{
    void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, ProblemContextByUnits context);
    void Visit(in CombustionSolverParamsByDoubles solverParams, ProblemContextByDoubles context);
}
```
