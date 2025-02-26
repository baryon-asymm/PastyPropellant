using ParametricCombustionModel.ReportMaking.PdfOperations;

namespace ParametricCombustionModel.ReportMaking.Interfaces;

public interface IPdfOperationVisitor
{
    public void Visit(
        AddTabOperation operation);

    public void Visit(
        LineBreakOperation operation);

    public void Visit(
        PrintTextOperation operation);
}
