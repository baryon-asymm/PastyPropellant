# API.md — Models

Пространство имён `ParametricCombustionModel.Optimization.Models`.

## Optimization problem, double tier ✅

```csharp
public class OptimizationProblemByDoubles : IOptimizationVisitable
{
    public ProblemContextByDoubles[,] ProblemContextMatrix;
    public double[,] ExperimentalBurnRates;
    public double FitnessFunctionValue;
    public double TotalEvaluatedPenalty;
    public Memory<IPenaltyEvaluator> PenaltyEvaluators;
    public Memory<double> EvaluatedPenalties;
    public ISolverVisitor Solver;

    public int PropellantCount { get; init; }
    public int PressureCount { get; init; }

    public OptimizationProblemByDoubles(
        ProblemContextByDoubles[,] problemContextMatrix,
        ISolverVisitor solver,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators);

    public virtual void Accept(in CombustionSolverParamsByUnits solverParamsByUnits, IFitnessFunctionVisitor fitnessFunction);  // бросает
    public virtual void Accept(in CombustionSolverParamsByDoubles solverParams, IFitnessFunctionVisitor fitnessFunction);
}
```

## Optimization problem, dimensional tier ✅

```csharp
public class OptimizationProblemByUnits : IOptimizationVisitable
{
    public ProblemContextByUnits[,] ProblemContextMatrix;
    public Speed[,] ExperimentalBurnRates;
    public double FitnessFunctionValue;
    public double TotalEvaluatedPenalty;
    public Memory<IPenaltyEvaluator> PenaltyEvaluators;
    public Memory<double> EvaluatedPenalties;
    public ISolverVisitor Solver;

    public int PropellantCount { get; init; }
    public int PressureCount { get; init; }

    public OptimizationProblemByUnits(
        ProblemContextByUnits[,] problemContextMatrix,
        ISolverVisitor solver,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators);

    public void Accept(in CombustionSolverParamsByUnits solverParamsByUnits, IFitnessFunctionVisitor fitnessFunction);
    public void Accept(in CombustionSolverParamsByDoubles solverParams, IFitnessFunctionVisitor fitnessFunction);  // бросает
}
```

## Single-composition result ✅

```csharp
public class OptimizationResult
{
    public ReadOnlySpan<double> LowerBound { get; }
    public ReadOnlySpan<double> UpperBound { get; }
    public ReadOnlySpan<double> BestSolverParams { get; }
    public CombustionSolverParamsByUnits BestSolverParamsByUnits { get; }
    public OptimizationProblemByUnits OptimizedContext { get; init; }

    public OptimizationResult(
        Memory<double> lowerBound,
        Memory<double> upperBound,
        Memory<double> bestParams,
        OptimizationProblemByUnits context);
}
```
