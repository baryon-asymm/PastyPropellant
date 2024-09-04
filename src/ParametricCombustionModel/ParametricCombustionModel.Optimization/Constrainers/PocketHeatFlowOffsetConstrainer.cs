using System.Runtime.CompilerServices;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;

namespace ParametricCombustionModel.Optimization.Constrainers;

public sealed class PocketHeatFlowOffsetConstrainer : BaseConstrainer
{
    public PocketHeatFlowOffsetConstrainer(double heatFlowsSegmentSize,
                                           double penaltyRate,
                                           IEnumerable<IEnumerable<MixedSolverParamsRecorder>> mixedSolverMatrix,
                                           ITargetFunctionSolver targetFunctionSolver,
                                           BaseConstrainer? nextConstrainer = default)
        : base(penaltyRate, mixedSolverMatrix, targetFunctionSolver, nextConstrainer)
    {
        HeatFlowsSegmentSize = heatFlowsSegmentSize;
    }

    public double HeatFlowsSegmentSize { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override double GetCurrentPenaltyValue(ref MixedComputationParams mixedComputationParams)
    {
        var pocketComputationParams = mixedComputationParams.PocketComputationParams;

        double maxHeatFlow, minHeatFlow;
        maxHeatFlow = minHeatFlow = pocketComputationParams.DiffusionFlameHeatFlow;

        HandleHeatFlow(pocketComputationParams.MetalBurningHeatFlow, ref maxHeatFlow, ref minHeatFlow);
        HandleHeatFlow(pocketComputationParams.SkeletonKineticFlameHeatFlow, ref maxHeatFlow, ref minHeatFlow);
        HandleHeatFlow(pocketComputationParams.OutSkeletonKineticFlameHeatFlow, ref maxHeatFlow, ref minHeatFlow);

        var currentHeatFlowsSegmentSize = maxHeatFlow / minHeatFlow;
        if (currentHeatFlowsSegmentSize > HeatFlowsSegmentSize)
            return PenaltyRate * currentHeatFlowsSegmentSize;

        return ZeroPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HandleHeatFlow(double heatFlow, ref double maxHeatFlow, ref double minHeatFlow)
    {
        if (heatFlow > maxHeatFlow)
            maxHeatFlow = heatFlow;
        else if (heatFlow < minHeatFlow)
            minHeatFlow = heatFlow;
    }
}
