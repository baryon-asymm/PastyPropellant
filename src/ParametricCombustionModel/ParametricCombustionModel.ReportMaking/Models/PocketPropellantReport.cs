namespace ParametricCombustionModel.ReportMaking.Models;

public record PocketPropellantReport(
    double SurfaceTemperature,
    double BurningRate,
    double DecomposingRate,
    double SkeletonKineticFlameHeatFlow,
    double SkeletonKineticFlameHeight,
    double OutSkeletonKineticFlameHeatFlow,
    double OutSkeletonKineticFlameHeight,
    double DiffusionFlameHeatFlow,
    double MetalBurningHeatFlow,
    double HeatFlowMaxMinDeviation,
    double BurningRateFraction,
    double PocketSurfaceFraction
);
