namespace ParametricCombustionModel.ReportMaking.Models;

public record InterPocketPropellantReport(
    double SurfaceTemperature,
    double BurningRate,
    double DecomposingRate,
    double KineticFlameHeatFlow,
    double KineticFlameHeight,
    double BurningRateFraction
);
