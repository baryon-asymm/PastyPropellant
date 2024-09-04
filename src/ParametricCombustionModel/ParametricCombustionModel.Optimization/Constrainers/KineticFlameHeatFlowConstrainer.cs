using System.Runtime.CompilerServices;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;

namespace ParametricCombustionModel.Optimization.Constrainers;

public class KineticFlameHeatFlowConstrainer : BaseConstrainer
{
    public KineticFlameHeatFlowConstrainer(double maxKineticFlameHeatFlow,
                                           double penaltyRate,
                                           IEnumerable<IEnumerable<MixedSolverParamsRecorder>> mixedSolverMatrix,
                                           ITargetFunctionSolver targetFunctionSolver,
                                           BaseConstrainer? nextConstrainer = default)
        : base(penaltyRate, mixedSolverMatrix, targetFunctionSolver, nextConstrainer)
    {
        if (maxKineticFlameHeatFlow <= 0)
            throw new ArgumentException("Max kinetic flame heat flow must be greater than 0");

        MaxKineticFlameHeatFlow = maxKineticFlameHeatFlow;
    }

    public double MaxKineticFlameHeatFlow { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override double GetCurrentPenaltyValue(ref MixedComputationParams mixedComputationParams)
    {
        var totalPenaltyValue = ZeroPenaltyValue;

        var interPocketParams = mixedComputationParams.InterPocketComputationParams;
        var pocketParams = mixedComputationParams.PocketComputationParams;

        HandleFlameHeatFlow(interPocketParams.KineticFlameHeatFlow / 10, ref totalPenaltyValue);
        HandleFlameHeatFlow(pocketParams.SkeletonKineticFlameHeatFlow, ref totalPenaltyValue);
        HandleFlameHeatFlow(pocketParams.OutSkeletonKineticFlameHeatFlow, ref totalPenaltyValue);

        return totalPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleFlameHeatFlow(double flameHeatFlow, ref double totalPenaltyValue)
    {
        if (flameHeatFlow > MaxKineticFlameHeatFlow)
            totalPenaltyValue += PenaltyRate * (flameHeatFlow / MaxKineticFlameHeatFlow);
    }
}
