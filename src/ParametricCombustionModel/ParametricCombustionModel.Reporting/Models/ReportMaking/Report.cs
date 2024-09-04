namespace ParametricCombustionModel.Reporting.Models.ReportMaking;

public record Report(
    IEnumerable<PropellantReport> PropellantReports
);
