using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.ProblemContexts;

namespace ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;

public sealed class LargeOxidizerParticleSizePenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public double LargeParticleDiameterThreshold { get; init; }

    public LargeOxidizerParticleSizePenaltyEvaluator(
        double penaltyRate,
        double largeParticleDiameterThreshold)
        : base(penaltyRate)
    {
        if (largeParticleDiameterThreshold <= 0.0)
            throw new ArgumentException("Large particle diameter threshold must be greater than 0.0");

        LargeParticleDiameterThreshold = largeParticleDiameterThreshold;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByUnits updatedProblemContext)
    {
        var skeletonLayerThickness = updatedProblemContext.PocketCombustionParams.SkeletonLayerThickness.Millimeters;
        var largeParticleDiameter = updatedProblemContext.PropellantParamsByUnits.AverageOxidizerDiameter.Millimeters;
        largeParticleDiameter *= LargeParticleDiameterThreshold;
        if (skeletonLayerThickness > largeParticleDiameter)
            return PenaltyRate * (skeletonLayerThickness / largeParticleDiameter);

        return ZeroPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByDoubles updatedProblemContext)
    {
        var skeletonLayerThickness = updatedProblemContext.PocketCombustionParams.SkeletonLayerThickness;
        var largeParticleDiameter = updatedProblemContext.PropellantParams.AverageOxidizerDiameter;
        largeParticleDiameter *= LargeParticleDiameterThreshold;
        if (skeletonLayerThickness > largeParticleDiameter)
            return PenaltyRate * (skeletonLayerThickness / largeParticleDiameter);

        return ZeroPenaltyValue;
    }
}
