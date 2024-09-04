using ParametricCombustionModel.PdfReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;

namespace ParametricCombustionModel.PdfReportMaking.Models;

public record PdfReport(
    PointPdfReport Point,
    double TargetFunctionValue,
    IEnumerable<PropellantPdfReport> PropellantReports
) : Report(Point, TargetFunctionValue, PropellantReports), ITransformable<IEnumerable<PdfLine>>
{
    public new PointPdfReport Point { get; init; }
    public new IEnumerable<PropellantPdfReport> PropellantReports { get; init; }

    public PdfReport(Report report) : this(GetPointPdfReport(report.Point),
                                           report.TargetFunctionValue,
                                           GetPropellantPdfReports(report.PropellantReports))
    {
        Point = GetPointPdfReport(report.Point);
        PropellantReports = GetPropellantPdfReports(report.PropellantReports);
    }

    public new IEnumerable<PdfLine> Transform()
    {
        var lines = new List<PdfLine>();

        lines.AddRange(Point.Transform());
        lines.Add(new PdfLine(LineStyle.None, string.Empty));

        lines.AddRange(GetLocalLines().Select(line => new PdfLine(LineStyle.Bold, line)));

        foreach (var propellantReport in PropellantReports)
        {
            lines.Add(new PdfLine(LineStyle.None, string.Empty));
            lines.AddRange(propellantReport.Transform());
        }

        return lines;
    }

    private static PointPdfReport GetPointPdfReport(PointReport pointReport)
    {
        return new PointPdfReport(pointReport);
    }

    private static IEnumerable<PropellantPdfReport> GetPropellantPdfReports(
        IEnumerable<PropellantReport> propellantReports)
    {
        return propellantReports.Select(propellantReport => new PropellantPdfReport(propellantReport));
    }
}
