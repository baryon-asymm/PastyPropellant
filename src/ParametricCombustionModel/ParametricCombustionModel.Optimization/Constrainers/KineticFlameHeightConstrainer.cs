using System.Runtime.CompilerServices;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;

namespace ParametricCombustionModel.Optimization.Constrainers;

public sealed class KineticFlameHeightConstrainer : BaseConstrainer
{
    public KineticFlameHeightConstrainer(double minFlameHeight,
                                         double maxFlameHeight,
                                         double penaltyRate,
                                         IEnumerable<IEnumerable<MixedSolverParamsRecorder>> mixedSolverMatrix,
                                         ITargetFunctionSolver targetFunctionSolver,
                                         BaseConstrainer? nextConstrainer = default)
        : base(penaltyRate, mixedSolverMatrix, targetFunctionSolver, nextConstrainer)
    {
        if (maxFlameHeight <= minFlameHeight)
            throw new ArgumentException("Max flame height must be greater than min flame height");

        MinFlameHeight = minFlameHeight;
        MaxFlameHeight = maxFlameHeight;
    }

    public double MinFlameHeight { get; init; }
    public double MaxFlameHeight { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override double GetCurrentPenaltyValue(ref MixedComputationParams mixedComputationParams)
    {
        var totalPenaltyValue = ZeroPenaltyValue;

        var interPocketParameters = mixedComputationParams.InterPocketComputationParams;
        var pocketParameters = mixedComputationParams.PocketComputationParams;

        HandleFlameHeight(interPocketParameters.KineticFlameHeight, ref totalPenaltyValue);
        HandleFlameHeight(pocketParameters.SkeletonKineticFlameHeight, ref totalPenaltyValue);
        HandleFlameHeight(pocketParameters.OutSkeletonKineticFlameHeight, ref totalPenaltyValue);

        return totalPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleFlameHeight(double flameHeight, ref double totalPenaltyValue)
    {
        if (flameHeight < MinFlameHeight)
            totalPenaltyValue += PenaltyRate * (MinFlameHeight / flameHeight);
        else if (flameHeight > MaxFlameHeight) totalPenaltyValue += PenaltyRate * (flameHeight / MaxFlameHeight);
    }
}
