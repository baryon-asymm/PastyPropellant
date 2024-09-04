namespace ParametricCombustionModel.Reporting.Models.ReportMaking;

public record InterPocketPropellantReport(
    double SurfaceTemperature,
    double BurningRate,
    double DecomposingRate,
    double KineticFlameHeatFlow,
    double BurningRateFraction
);
