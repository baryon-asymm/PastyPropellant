using ParametricCombustionModel.ReportMaking.Interfaces;

namespace ParametricCombustionModel.ReportMaking.PdfOperations;

public class LineBreakOperation : IPdfOperation
{
    public void Accept(
        IPdfOperationVisitor visitor) =>
        visitor.Visit(this);
}
