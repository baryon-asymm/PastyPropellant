using System.Text;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Resources;

namespace ParametricCombustionModel.ReportMaking.Reports.Text;

public class FitnessFunctionEvaluatorReport : BaseReport, ITransformable<string>
{
    public FitnessFunctionEvaluatorReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public string Transform()
    {
        var stringBuilder = new StringBuilder();
        var optimizedContext = Result.OptimizedContext;

        stringBuilder.AppendLine(FitnessFunctionEvaluatorReportResources.HeaderOfFitnessFunctionEvaluator);
        stringBuilder.AppendFormat(FitnessFunctionEvaluatorReportResources.FitnessFunctionValue,
                                   optimizedContext.FitnessFunctionValue);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(FitnessFunctionEvaluatorReportResources.TotalEvaluatedPenalty,
                                   optimizedContext.TotalEvaluatedPenalty);
        stringBuilder.AppendLine();

        return stringBuilder.ToString();
    }
}
