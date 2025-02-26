using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Reporting.Models.ReportMaking;

public record PropellantReport(
    Propellant Propellant,
    double PocketPropellantVolumeFraction,
    double InterPocketPropellantVolumeFraction,
    IEnumerable<PressureFrameReport> PressureFrameReports
);
