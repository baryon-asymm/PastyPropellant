namespace ParametricCombustionModel.Core.DTOs;

public record OptimizationResult(
    double TargetFunctionValue,
    IEnumerable<double> FinalPoint
);
