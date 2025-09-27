using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.Models;

namespace ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;

public class PenaltyFitnessFunctionEvaluator : FitnessFunctionEvaluator
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        OptimizationProblemByUnits context)
    {
        FlushPenalties(context);
        base.Visit(in solverParamsByUnits, context);

        if (context.FitnessFunctionValue == double.MaxValue)
            return;

        var penaltyEvaluators = context.PenaltyEvaluators.Span;
        var evaluatedPenalties = context.EvaluatedPenalties.Span;

        for (var i = 0; i < evaluatedPenalties.Length; i++)
        {
            var penalty = 0.0;
            for (var j = 0; j < context.PropellantCount; j++)
            {
                for (var k = 0; k < context.PressureCount; k++)
                {
                    penalty += penaltyEvaluators[i].GetPenaltyValue(
                        context.ProblemContextMatrix[j, k]);
                }
            }

            evaluatedPenalties[i] = penalty;
            context.TotalEvaluatedPenalty += penalty;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void FlushPenalties(
        OptimizationProblemByUnits context)
    {
        var penaltyEvaluators = context.PenaltyEvaluators.Span;
        var evaluatedPenalties = context.EvaluatedPenalties.Span;

        context.TotalEvaluatedPenalty = 0.0;
        for (var i = 0; i < evaluatedPenalties.Length; i++)
            evaluatedPenalties[i] = 0.0;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        OptimizationProblemByDoubles context)
    {
        FlushPenalties(context);
        base.Visit(in solverParams, context);

        if (context.FitnessFunctionValue == double.MaxValue)
            return;

        var penaltyEvaluators = context.PenaltyEvaluators.Span;
        var evaluatedPenalties = context.EvaluatedPenalties.Span;

        for (var i = 0; i < evaluatedPenalties.Length; i++)
        {
            var penalty = 0.0;
            for (var j = 0; j < context.PropellantCount; j++)
            {
                for (var k = 0; k < context.PressureCount; k++)
                {
                    penalty += penaltyEvaluators[i].GetPenaltyValue(
                        context.ProblemContextMatrix[j, k]);
                }
            }

            evaluatedPenalties[i] = penalty;
            context.TotalEvaluatedPenalty += penalty;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void FlushPenalties(
        OptimizationProblemByDoubles context)
    {
        var penaltyEvaluators = context.PenaltyEvaluators.Span;
        var evaluatedPenalties = context.EvaluatedPenalties.Span;

        context.TotalEvaluatedPenalty = 0.0;
        for (var i = 0; i < evaluatedPenalties.Length; i++)
            evaluatedPenalties[i] = 0.0;
    }
}
