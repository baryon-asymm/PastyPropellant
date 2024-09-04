using PDFsharp.Api.Models;

namespace ParametricCombustionModel.PdfReportMaking.Models;

public record PdfContentHolder(
    IEnumerable<PdfLine> Title,
    IEnumerable<PdfLine> Body,
    IEnumerable<PressureTable> Tables,
    IEnumerable<string> Images,
    IEnumerable<PdfLine> Footer
);
