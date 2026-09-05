# API.md — Interfaces

Пространство имён
`ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces`.

## Penalty contract ✅

```csharp
public interface IPenaltyEvaluator
{
    double GetPenaltyValue(ProblemContextByUnits updatedProblemContext);
    double GetPenaltyValue(ProblemContextByDoubles updatedProblemContext);
}
```
