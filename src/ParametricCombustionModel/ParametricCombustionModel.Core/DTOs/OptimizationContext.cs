using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Core.DTOs;

public record OptimizationContext(
    IEnumerable<double> Pressures,
    IEnumerable<double> InitialPoint,
    int NumTrialPoints,
    int NumStageOnePoints,
    IEnumerable<double> LowerBound,
    IEnumerable<double> UpperBound,
    double MinSurfaceTemperature,
    double MaxSurfaceTemperature,
    IEnumerable<Propellant> Propellants,
    TimeSpan? MaxTime = null,
    double MiddlePointHeatFlowMultipleOffset = 10
);
