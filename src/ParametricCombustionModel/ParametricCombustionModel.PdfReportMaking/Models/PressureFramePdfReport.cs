using ParametricCombustionModel.PdfReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;

namespace ParametricCombustionModel.PdfReportMaking.Models;

public record PressureFramePdfReport(
    double Pressure,
    double BurningRate,
    double ExperimentalBurningRate,
    PocketPropellantReport PocketPropellantReport,
    InterPocketPropellantReport InterPocketPropellantReport
) : PressureFrameReport(Pressure,
                        BurningRate,
                        ExperimentalBurningRate,
                        PocketPropellantReport,
                        InterPocketPropellantReport),
    ITransformable<IEnumerable<PdfLine>>
{
    public PressureFramePdfReport(PressureFrameReport report) : this(report.Pressure,
                                                                     report.BurningRate,
                                                                     report.ExperimentalBurningRate,
                                                                     report.PocketPropellantReport,
                                                                     report.InterPocketPropellantReport)
    {
    }

    public new IEnumerable<PdfLine> Transform()
    {
        int[] italicLines = [0, 3, 10];
        int[] underlinedLines = [0];

        var i = 0;
        var lines = GetLocalLines()
                    .Select(line =>
                    {
                        var style = LineStyle.None;
                        if (italicLines.Contains(i))
                            style |= LineStyle.Italic;
                        if (underlinedLines.Contains(i))
                            style |= LineStyle.Underline;
                        i++;
                        return new PdfLine(style, line.Replace("    ", "\t"));
                    })
                    .ToList();

        return lines;
    }
}
