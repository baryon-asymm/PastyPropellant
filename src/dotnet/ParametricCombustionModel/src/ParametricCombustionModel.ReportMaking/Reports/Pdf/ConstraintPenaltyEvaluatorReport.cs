using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class ConstraintPenaltyEvaluatorReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public ConstraintPenaltyEvaluatorReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();
        var penaltyEvaluators = GetPenaltyEvaluators();

        operations.Enqueue(new PrintTextOperation(
                               ConstraintPenaltyEvaluatorReportResources.HeaderOfConstraintPenaltyEvaluators,
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
        foreach (var penaltyEvaluator in penaltyEvaluators)
        {
            switch (penaltyEvaluator)
            {
                case InterPocketFasterBurnPenaltyEvaluator evaluator:
                    AppendInterPocketFasterBurnPenaltyEvaluator(operations,
                                                                evaluator);
                    break;
                case PocketHeatFluxRatioCompetitionPenaltyEvaluator evaluator:
                    AppendPocketHeatFluxRatioCompetitionPenaltyEvaluator(operations,
                                                                         evaluator);
                    break;
                case KineticFlameHeatFluxPenaltyEvaluator evaluator:
                    AppendKineticFlameHeatFluxPenaltyEvaluator(operations,
                                                               evaluator);
                    break;
                case PoreDiameterPenaltyEvaluator evaluator:
                    AppendPoreDiameterPenaltyEvaluator(operations,
                                                       evaluator);
                    break;
                default:
                    EventBus<InfoLogEvent>.Publish(
                        new InfoLogEvent(
                            $"Unsupported penalty evaluator type: {penaltyEvaluator.GetType().Name}."));
                    break;
            }
        }

        return operations;
    }

    private IEnumerable<IPenaltyEvaluator> GetPenaltyEvaluators()
    {
        return Result.OptimizedContext.PenaltyEvaluators.ToArray().AsReadOnly();
    }

    private static void AppendInterPocketFasterBurnPenaltyEvaluator(
        Queue<IPdfOperation> operations,
        InterPocketFasterBurnPenaltyEvaluator penaltyEvaluator)
    {
        operations.Enqueue(new PrintTextOperation(
                               ConstraintPenaltyEvaluatorReportResources.InterPocketFasterBurnPenaltyEvaluator,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ConstraintPenaltyEvaluatorReportResources.PenaltyRate,
                                             penaltyEvaluator.PenaltyRate),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
    }

    private static void AppendPocketHeatFluxRatioCompetitionPenaltyEvaluator(
        Queue<IPdfOperation> operations,
        PocketHeatFluxRatioCompetitionPenaltyEvaluator penaltyEvaluator)
    {
        operations.Enqueue(new PrintTextOperation(
                               ConstraintPenaltyEvaluatorReportResources
                                   .PocketHeatFluxRatioCompetitionPenaltyEvaluator,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ConstraintPenaltyEvaluatorReportResources.PenaltyRate,
                                             penaltyEvaluator.PenaltyRate),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ConstraintPenaltyEvaluatorReportResources.HeatFluxRatioThreshold,
                                             penaltyEvaluator.HeatFluxRatioThreshold),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
    }

    private static void AppendKineticFlameHeatFluxPenaltyEvaluator(
        Queue<IPdfOperation> operations,
        KineticFlameHeatFluxPenaltyEvaluator penaltyEvaluator)
    {
        operations.Enqueue(new PrintTextOperation(
                               ConstraintPenaltyEvaluatorReportResources.KineticFlameHeatFluxPenaltyEvaluator,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ConstraintPenaltyEvaluatorReportResources.PenaltyRate,
                                             penaltyEvaluator.PenaltyRate),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(
                                   ConstraintPenaltyEvaluatorReportResources.MaxInterPocketKineticFlameHeatFlux,
                                   penaltyEvaluator.MaxInterPocketKineticFlameHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ConstraintPenaltyEvaluatorReportResources.MaxSkeletonKineticFlameHeatFlux,
                                             penaltyEvaluator.MaxSkeletonKineticFlameHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(
                                   ConstraintPenaltyEvaluatorReportResources.MaxOutSkeletonKineticFlameHeatFlux,
                                   penaltyEvaluator.MaxOutSkeletonKineticFlameHeatFlux),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
    }

    private static void AppendPoreDiameterPenaltyEvaluator(
        Queue<IPdfOperation> operations,
        PoreDiameterPenaltyEvaluator penaltyEvaluator)
    {
        operations.Enqueue(new PrintTextOperation(
                               ConstraintPenaltyEvaluatorReportResources.PoreDiameterPenaltyEvaluator,
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ConstraintPenaltyEvaluatorReportResources.PenaltyRate,
                                             penaltyEvaluator.PenaltyRate),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(ConstraintPenaltyEvaluatorReportResources.PoreDiameterThreshold,
                                             penaltyEvaluator.PoreDiameterThreshold),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
    }
}
