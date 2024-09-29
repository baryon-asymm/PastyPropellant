using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;
using UnitsNet;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class ParametricConstraintReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public ParametricConstraintReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();

        operations.Enqueue(new PrintTextOperation(
                               ParametricConstraintReportResources.HeaderOfParametricConstraint,
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               ParametricConstraintReportResources.SurfaceTemperatureRangeConstraint,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ParametricConstraintReportResources.MinSurfaceTemperature,
                                             GetMinSurfaceTemperature(
                                                 Result.OptimizedContext.ProblemContextMatrix[0, 0])),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ParametricConstraintReportResources.MaxSurfaceTemperature,
                                             GetMaxSurfaceTemperature(
                                                 Result.OptimizedContext.ProblemContextMatrix[0, 0])),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        return operations;
    }

    private static Temperature GetMinSurfaceTemperature(
        ProblemContextByUnits problemContext)
    {
        return problemContext.MinSurfaceTemperature;
    }

    private static Temperature GetMaxSurfaceTemperature(
        ProblemContextByUnits problemContext)
    {
        return problemContext.MaxSurfaceTemperature;
    }
}
