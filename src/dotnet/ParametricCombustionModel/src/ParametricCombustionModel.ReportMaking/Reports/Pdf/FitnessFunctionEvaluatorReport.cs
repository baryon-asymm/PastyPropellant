using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class FitnessFunctionEvaluatorReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public FitnessFunctionEvaluatorReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();
        var optimizedContext = Result.OptimizedContext;

        operations.Enqueue(new PrintTextOperation(
                               FitnessFunctionEvaluatorReportResources.HeaderOfFitnessFunctionEvaluator,
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(FitnessFunctionEvaluatorReportResources.FitnessFunctionValue,
                                             optimizedContext.FitnessFunctionValue),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(FitnessFunctionEvaluatorReportResources.TotalEvaluatedPenalty,
                                             optimizedContext.TotalEvaluatedPenalty),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        return operations;
    }
}
