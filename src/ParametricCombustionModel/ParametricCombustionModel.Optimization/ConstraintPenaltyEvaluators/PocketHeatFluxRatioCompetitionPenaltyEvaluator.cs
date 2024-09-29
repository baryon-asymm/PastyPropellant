using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.ProblemContexts;

namespace ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;

public sealed class PocketHeatFluxRatioCompetitionPenaltyEvaluator : BaseConstraintPenaltyEvaluator
{
    public double HeatFluxRatioThreshold { get; init; }

    public PocketHeatFluxRatioCompetitionPenaltyEvaluator(
        double penaltyRate,
        double heatFluxRatioThreshold)
        : base(penaltyRate)
    {
        if (heatFluxRatioThreshold <= 1.0)
            throw new ArgumentException("Heat flux ratio threshold must be greater than 1.0 (max/min)");

        HeatFluxRatioThreshold = heatFluxRatioThreshold;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByUnits updatedProblemContext)
    {
        double maxHeatFlow, minHeatFlow;
        ref var pocketParams = ref updatedProblemContext.PocketCombustionParams;
        maxHeatFlow = minHeatFlow = pocketParams.DiffusionFlameHeatFlux.WattsPerSquareMeter;
        ref var skeletonParams = ref pocketParams.SkeletonKineticFlameCombustionParams;
        ref var outSkeletonParams = ref pocketParams.OutSkeletonKineticFlameCombustionParams;

        // ExtractMaxMinHeatFluxes(pocketParams.MetalBurningHeatFlux.WattsPerSquareMeter, ref maxHeatFlow,
        //                         ref minHeatFlow);
        ExtractMaxMinHeatFluxes(skeletonParams.KineticFlameHeatFlux.WattsPerSquareMeter, ref maxHeatFlow,
                                ref minHeatFlow);
        ExtractMaxMinHeatFluxes(outSkeletonParams.KineticFlameHeatFlux.WattsPerSquareMeter, ref maxHeatFlow,
                                ref minHeatFlow);

        var actualHeatFluxRatio = maxHeatFlow / minHeatFlow;
        var a = 0.0;
        var b = 0.0;
        if (pocketParams.MetalBurningHeatFlux.WattsPerSquareMeter < maxHeatFlow)
            a = PenaltyRate * maxHeatFlow / pocketParams.MetalBurningHeatFlux.WattsPerSquareMeter;
        if (actualHeatFluxRatio > HeatFluxRatioThreshold)
            b = PenaltyRate * actualHeatFluxRatio;
        return a + b;

        return ZeroPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override double GetPenaltyValue(
        ProblemContextByDoubles updatedProblemContext)
    {
        double minHeatFlow;
        ref var pocketParams = ref updatedProblemContext.PocketCombustionParams;
        var maxHeatFlow = minHeatFlow = pocketParams.DiffusionFlameHeatFlux;
        ref var skeletonParams = ref pocketParams.SkeletonKineticFlameCombustionParams;
        ref var outSkeletonParams = ref pocketParams.OutSkeletonKineticFlameCombustionParams;

        // ExtractMaxMinHeatFluxes(pocketParams.MetalBurningHeatFlux, ref maxHeatFlow,
        //                         ref minHeatFlow);
        ExtractMaxMinHeatFluxes(skeletonParams.KineticFlameHeatFlux, ref maxHeatFlow,
                                ref minHeatFlow);
        ExtractMaxMinHeatFluxes(outSkeletonParams.KineticFlameHeatFlux, ref maxHeatFlow,
                                ref minHeatFlow);

        var actualHeatFluxRatio = maxHeatFlow / minHeatFlow;
        var a = 0.0;
        var b = 0.0;
        if (pocketParams.MetalBurningHeatFlux < maxHeatFlow)
            a = PenaltyRate * maxHeatFlow / pocketParams.MetalBurningHeatFlux;
        if (actualHeatFluxRatio > HeatFluxRatioThreshold)
            b = PenaltyRate * actualHeatFluxRatio;
        return a + b;

        return ZeroPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void ExtractMaxMinHeatFluxes(
        double heatFlux,
        ref double maxHeatFlux,
        ref double minHeatFlux)
    {
        if (heatFlux >= maxHeatFlux)
            maxHeatFlux = heatFlux;
        else if (heatFlux <= minHeatFlux)
            minHeatFlux = heatFlux;
    }
}
