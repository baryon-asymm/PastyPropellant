namespace ParametricCombustionModel.Reporting.Models.ReportMaking;

public record PocketPropellantReport(
    double SurfaceTemperature,
    double BurningRate,
    double DecomposingRate,
    double SkeletonKineticFlameHeatFlow,
    double OutSkeletonKineticFlameHeatFlow,
    double DiffusionFlameHeatFlow,
    double MetalBurningHeatFlow,
    double BurningRateFraction
);
