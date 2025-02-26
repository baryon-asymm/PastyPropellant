using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;

namespace ParametricCombustionModel.ReportMaking.PdfOperations;

public class PrintTextOperation : IPdfOperation
{
    public TextStyle Style { get; init; }

    public string Text { get; init; }

    public PrintTextOperation(
        string text,
        TextStyle style)
    {
        Text = text;
        Style = style;
    }

    public void Accept(
        IPdfOperationVisitor visitor) =>
        visitor.Visit(this);
}
