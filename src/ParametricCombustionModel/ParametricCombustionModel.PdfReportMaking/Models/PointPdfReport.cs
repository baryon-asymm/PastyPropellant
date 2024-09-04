using System.Collections.ObjectModel;
using ParametricCombustionModel.PdfReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;

namespace ParametricCombustionModel.PdfReportMaking.Models;

public record PointPdfReport(
    ReadOnlyCollection<double> Vector
) : PointReport(Vector), ITransformable<IEnumerable<PdfLine>>
{
    public PointPdfReport(PointReport pointReport) : this(pointReport.Vector)
    {
    }
    
    public new IEnumerable<PdfLine> Transform()
    {
        return GetLocalLines().Select(line => new PdfLine(LineStyle.None, line));
    }
}
