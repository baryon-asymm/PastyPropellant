using System.Text;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Reports.Text;

namespace ParametricCombustionModel.ReportMaking.ReportMakers;

public class TextReportMaker : IReportMaker
{
    private readonly OptimizationResult _optimizationResult;

    private TextReportMaker(
        OptimizationResult optimizationResult)
    {
        _optimizationResult = optimizationResult ?? throw new ArgumentNullException(nameof(optimizationResult));
    }

    public string MakeReport()
    {
        var stringBuilder = new StringBuilder();
        var propellantReport = new PropellantReport(_optimizationResult);
        var penaltyEvaluatorsReport = new ConstraintPenaltyEvaluatorReport(_optimizationResult);
        var parametricConstraintReport = new ParametricConstraintReport(_optimizationResult);
        var combustionSolverParamsReport = new CombustionSolverParamsReport(_optimizationResult);
        var fitnessFunctionEvaluatorReport = new FitnessFunctionEvaluatorReport(_optimizationResult);
        var problemContextReport = new ProblemContextReport(3, _optimizationResult);

        stringBuilder.AppendLine(propellantReport.Transform());
        stringBuilder.AppendLine(penaltyEvaluatorsReport.Transform());
        stringBuilder.AppendLine(parametricConstraintReport.Transform());
        stringBuilder.AppendLine(combustionSolverParamsReport.Transform());
        stringBuilder.AppendLine(fitnessFunctionEvaluatorReport.Transform());
        stringBuilder.AppendLine(problemContextReport.Transform());

        return stringBuilder.ToString();
    }

    public static TextReportMaker FromOptimizationResult(
        OptimizationResult optimizationResult)
    {
        return new TextReportMaker(optimizationResult);
    }
}
