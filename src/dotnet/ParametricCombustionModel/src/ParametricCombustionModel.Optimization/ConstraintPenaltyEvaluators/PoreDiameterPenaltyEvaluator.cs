using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.ProblemContexts;

namespace ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;

public sealed class PoreDiameterPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public double PoreDiameterThreshold { get; init; }

    public PoreDiameterPenaltyEvaluator(
        double penaltyRate,
        double poreDiameterThreshold)
        : base(penaltyRate)
    {
        if (poreDiameterThreshold <= 0.0)
            throw new ArgumentException("Pore diameter threshold must be greater than 0.0");

        PoreDiameterThreshold = poreDiameterThreshold;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByUnits updatedProblemContext)
    {
        var poreDiameter = updatedProblemContext.PocketCombustionParams.PoreDiameter.Millimeters;
        var skeletonLayerThickness = updatedProblemContext.PocketCombustionParams.SkeletonLayerThickness.Millimeters;
        poreDiameter *= PoreDiameterThreshold;
        if (skeletonLayerThickness < poreDiameter)
            return PenaltyRate * (poreDiameter / skeletonLayerThickness);

        return ZeroPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByDoubles updatedProblemContext)
    {
        var poreDiameter = updatedProblemContext.PocketCombustionParams.PoreDiameter;
        var skeletonLayerThickness = updatedProblemContext.PocketCombustionParams.SkeletonLayerThickness;
        poreDiameter *= PoreDiameterThreshold;
        if (skeletonLayerThickness < poreDiameter)
            return PenaltyRate * (poreDiameter / skeletonLayerThickness);

        return ZeroPenaltyValue;
    }
}
