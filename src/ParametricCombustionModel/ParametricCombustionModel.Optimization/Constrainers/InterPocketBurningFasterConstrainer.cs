using System.Runtime.CompilerServices;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;

namespace ParametricCombustionModel.Optimization.Constrainers;

public sealed class InterPocketBurningFasterConstrainer : BaseConstrainer
{
    public InterPocketBurningFasterConstrainer(double penaltyRate,
                                               IEnumerable<IEnumerable<MixedSolverParamsRecorder>> mixedSolverMatrix,
                                               ITargetFunctionSolver targetFunctionSolver,
                                               BaseConstrainer nextConstrainer)
        : base(penaltyRate, mixedSolverMatrix, targetFunctionSolver, nextConstrainer)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override double GetCurrentPenaltyValue(ref MixedComputationParams mixedComputationParams)
    {
        var interPocketParameters = mixedComputationParams.InterPocketComputationParams;
        var pocketParameters = mixedComputationParams.PocketComputationParams;

        var interPocketBurningRate = interPocketParameters.BurningRate;
        var pocketBurningRate = pocketParameters.BurningRate;

        if (pocketBurningRate > interPocketBurningRate)
        {
            var ratio = pocketBurningRate / interPocketBurningRate;
            return PenaltyRate * ratio;
        }

        return ZeroPenaltyValue;
    }
}
