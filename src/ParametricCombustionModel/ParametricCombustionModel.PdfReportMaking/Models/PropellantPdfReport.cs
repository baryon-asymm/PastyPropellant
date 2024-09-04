using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.PdfReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;

namespace ParametricCombustionModel.PdfReportMaking.Models;

public record PropellantPdfReport(
    Propellant Propellant,
    double PocketPropellantVolumeFraction,
    double InterPocketPropellantVolumeFraction,
    IEnumerable<PressureFramePdfReport> PressureFrameReports
) : PropellantReport(Propellant,
                     PocketPropellantVolumeFraction,
                     InterPocketPropellantVolumeFraction,
                     PressureFrameReports),
    ITransformable<IEnumerable<PdfLine>>
{
    public new IEnumerable<PressureFramePdfReport> PressureFrameReports { get; init; }

    public PropellantPdfReport(PropellantReport report) : this(report.Propellant,
                                                               report.PocketPropellantVolumeFraction,
                                                               report.InterPocketPropellantVolumeFraction,
                                                               GetPressureFramePdfReports(report.PressureFrameReports))
    {
        PressureFrameReports = GetPressureFramePdfReports(report.PressureFrameReports);
    }

    public new IEnumerable<PdfLine> Transform()
    {
        int[] boldedLines = [0];

        var i = 0;
        var lines = GetLocalLines()
                    .Select(line => boldedLines.Contains(i++)
                                ? new PdfLine(LineStyle.Bold, line)
                                : new PdfLine(LineStyle.None, line))
                    .ToList();

        foreach (var pressureFrameReport in PressureFrameReports)
        {
            lines.Add(new PdfLine(LineStyle.None, string.Empty));
            lines.AddRange(pressureFrameReport.Transform());
        }

        return lines;
    }

    private static IEnumerable<PressureFramePdfReport> GetPressureFramePdfReports(
        IEnumerable<PressureFrameReport> pressureFrameReports)
    {
        return pressureFrameReports.Select(pressureFrameReport => new PressureFramePdfReport(pressureFrameReport));
    }
}
