using System.Text;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Resources;

namespace ParametricCombustionModel.ReportMaking.Reports.Text;

public class ConstraintPenaltyEvaluatorReport : BaseReport, ITransformable<string>
{
    public ConstraintPenaltyEvaluatorReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public string Transform()
    {
        var stringBuilder = new StringBuilder();
        var penaltyEvaluators = GetPenaltyEvaluators();

        stringBuilder.AppendLine(ConstraintPenaltyEvaluatorReportResources.HeaderOfConstraintPenaltyEvaluators);
        foreach (var penaltyEvaluator in penaltyEvaluators)
        {
            switch (penaltyEvaluator)
            {
                case InterPocketFasterBurnPenaltyEvaluator evaluator:
                    AppendInterPocketFasterBurnPenaltyEvaluator(stringBuilder,
                                                                evaluator);
                    break;
                case PocketHeatFluxRatioCompetitionPenaltyEvaluator evaluator:
                    AppendPocketHeatFluxRatioCompetitionPenaltyEvaluator(stringBuilder,
                                                                         evaluator);
                    break;
                case KineticFlameHeatFluxPenaltyEvaluator evaluator:
                    AppendKineticFlameHeatFluxPenaltyEvaluator(stringBuilder,
                                                               evaluator);
                    break;
            }
        }

        return stringBuilder.ToString();
    }

    private IEnumerable<IPenaltyEvaluator> GetPenaltyEvaluators()
    {
        return Result.OptimizedContext.PenaltyEvaluators.ToArray().AsReadOnly();
    }

    private static void AppendInterPocketFasterBurnPenaltyEvaluator(
        StringBuilder stringBuilder,
        InterPocketFasterBurnPenaltyEvaluator penaltyEvaluator)
    {
        stringBuilder.AppendLine(ConstraintPenaltyEvaluatorReportResources.InterPocketFasterBurnPenaltyEvaluator);
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ConstraintPenaltyEvaluatorReportResources.PenaltyRate,
                                   penaltyEvaluator.PenaltyRate);
        stringBuilder.AppendLine();
    }

    private static void AppendPocketHeatFluxRatioCompetitionPenaltyEvaluator(
        StringBuilder stringBuilder,
        PocketHeatFluxRatioCompetitionPenaltyEvaluator penaltyEvaluator)
    {
        stringBuilder.AppendLine(ConstraintPenaltyEvaluatorReportResources
                                     .PocketHeatFluxRatioCompetitionPenaltyEvaluator);
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ConstraintPenaltyEvaluatorReportResources.PenaltyRate,
                                   penaltyEvaluator.PenaltyRate);
        stringBuilder.AppendLine();
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ConstraintPenaltyEvaluatorReportResources.HeatFluxRatioThreshold,
                                   penaltyEvaluator.HeatFluxRatioThreshold);
        stringBuilder.AppendLine();
    }

    private static void AppendKineticFlameHeatFluxPenaltyEvaluator(
        StringBuilder stringBuilder,
        KineticFlameHeatFluxPenaltyEvaluator penaltyEvaluator)
    {
        stringBuilder.AppendLine(ConstraintPenaltyEvaluatorReportResources.KineticFlameHeatFluxPenaltyEvaluator);
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ConstraintPenaltyEvaluatorReportResources.PenaltyRate,
                                   penaltyEvaluator.PenaltyRate);
        stringBuilder.AppendLine();
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ConstraintPenaltyEvaluatorReportResources.MaxInterPocketKineticFlameHeatFlux,
                                   penaltyEvaluator.MaxInterPocketKineticFlameHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ConstraintPenaltyEvaluatorReportResources.MaxSkeletonKineticFlameHeatFlux,
                                   penaltyEvaluator.MaxSkeletonKineticFlameHeatFlux);
        stringBuilder.AppendLine();
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ConstraintPenaltyEvaluatorReportResources.MaxOutSkeletonKineticFlameHeatFlux,
                                   penaltyEvaluator.MaxOutSkeletonKineticFlameHeatFlux);
        stringBuilder.AppendLine();
    }
}
