using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.ProblemContexts;

namespace ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;

public sealed class InterPocketFasterBurnPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public InterPocketFasterBurnPenaltyEvaluator(
        double penaltyRate)
        : base(penaltyRate)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByUnits updatedProblemContext)
    {
        ref var interPocketCombustionParams = ref updatedProblemContext.InterPocketCombustionParams;
        ref var pocketCombustionParams = ref updatedProblemContext.PocketCombustionParams;

        return GetPenaltyValue(
            pocketCombustionParams.BurnRate.MetersPerSecond,
            interPocketCombustionParams.BurnRate.MetersPerSecond);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByDoubles updatedProblemContext)
    {
        ref var interPocketCombustionParams = ref updatedProblemContext.InterPocketCombustionParams;
        ref var pocketCombustionParams = ref updatedProblemContext.PocketCombustionParams;

        return GetPenaltyValue(
            pocketCombustionParams.BurnRate,
            interPocketCombustionParams.BurnRate);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetPenaltyValue(
        double pocketBurnRate,
        double interPocketBurnRate)
    {
        if (pocketBurnRate >= interPocketBurnRate)
        {
            var ratio = pocketBurnRate / interPocketBurnRate;
            return PenaltyRate * ratio;
        }

        return ZeroPenaltyValue;
    }
}
