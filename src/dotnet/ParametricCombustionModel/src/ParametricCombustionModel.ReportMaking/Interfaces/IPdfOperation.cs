namespace ParametricCombustionModel.ReportMaking.Interfaces;

public interface IPdfOperation
{
    public void Accept(
        IPdfOperationVisitor visitor);
}
