namespace ParametricCombustionModel.Reporting.Models.ReportMaking;

public record PressureFrameReport(
    double Pressure,
    double BurningRate,
    double ExperimentalBurningRate,
    PocketPropellantReport PocketPropellantReport,
    InterPocketPropellantReport InterPocketPropellantReport
);
