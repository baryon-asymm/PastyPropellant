using ParametricCombustionModel.PdfReportMaking.Enums;

namespace ParametricCombustionModel.PdfReportMaking.Models;

public record PdfLine(
    LineStyle Style,
    string Text
);
