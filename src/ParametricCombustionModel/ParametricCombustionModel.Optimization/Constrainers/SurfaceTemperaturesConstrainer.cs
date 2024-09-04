using System.Runtime.CompilerServices;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;

namespace ParametricCombustionModel.Optimization.Constrainers;

public class SurfaceTemperaturesConstrainer : BaseConstrainer
{
    public SurfaceTemperaturesConstrainer(double minSurfaceTemperature,
                                          double maxSurfaceTemperature,
                                          double penaltyRate,
                                          IEnumerable<IEnumerable<MixedSolverParamsRecorder>> mixedSolverMatrix,
                                          ITargetFunctionSolver targetFunctionSolver,
                                          BaseConstrainer? nextConstrainer = default)
        : base(penaltyRate, mixedSolverMatrix, targetFunctionSolver, nextConstrainer)
    {
        if (maxSurfaceTemperature <= minSurfaceTemperature)
            throw new ArgumentException("Max surface temperature must be greater than min surface temperature");

        MinSurfaceTemperature = minSurfaceTemperature;
        MaxSurfaceTemperature = maxSurfaceTemperature;
    }

    public double MinSurfaceTemperature { get; init; }
    public double MaxSurfaceTemperature { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override double GetCurrentPenaltyValue(ref MixedComputationParams mixedComputationParams)
    {
        var totalPenaltyValue = ZeroPenaltyValue;

        var interPocketParameters = mixedComputationParams.InterPocketComputationParams;
        var pocketParameters = mixedComputationParams.PocketComputationParams;

        HandleSurfaceTemperature(interPocketParameters.SurfaceTemperature, ref totalPenaltyValue);
        HandleSurfaceTemperature(pocketParameters.SurfaceTemperature, ref totalPenaltyValue);

        return totalPenaltyValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleSurfaceTemperature(double surfaceTemperature, ref double totalPenaltyValue)
    {
        if (surfaceTemperature < MinSurfaceTemperature)
            totalPenaltyValue += PenaltyRate * (MinSurfaceTemperature / surfaceTemperature);
        else if (surfaceTemperature > MaxSurfaceTemperature)
            totalPenaltyValue += PenaltyRate * (surfaceTemperature / MaxSurfaceTemperature);
    }
}
