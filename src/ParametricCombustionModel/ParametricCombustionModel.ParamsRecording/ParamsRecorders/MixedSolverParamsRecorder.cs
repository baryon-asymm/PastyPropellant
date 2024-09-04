using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.ParamsRecording.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;

namespace ParametricCombustionModel.ParamsRecording.ParamsRecorders;

public class MixedSolverParamsRecorder : MixedPropellantSolver, IParamsRecorder<MixedComputationParams>
{
    public MixedSolverParamsRecorder(double pressure,
                                     double interPocketVolumeFraction,
                                     double pocketVolumeFraction,
                                     InterPocketSolverParamsRecorder interPocketRecorder,
                                     PocketSolverParamsRecorder pocketParamsRecorder)
        : base(pressure,
               interPocketVolumeFraction,
               pocketVolumeFraction,
               interPocketRecorder,
               pocketParamsRecorder)
    {
    }

    public MixedComputationParams GetRecord(Span<double> surfaceTemperatures, ref BurningParams burningParams)
    {
        var interPocketRecorder = InterPocketPropellantSolver as InterPocketSolverParamsRecorder;
        var pocketRecorder = PocketParamsPropellantSolver as PocketSolverParamsRecorder;

        var interPocketParams = interPocketRecorder!.GetRecord(surfaceTemperatures, ref burningParams);
        var pocketParams = pocketRecorder!.GetRecord(surfaceTemperatures, ref burningParams);

        var burningRate = GetBurningRate(interPocketParams.BurningRate, pocketParams.BurningRate);

        var interPocketBurningRateFraction = burningRate / (interPocketParams.BurningRate / InterPocketVolumeFraction);
        var pocketBurningRateFraction = burningRate / (pocketParams.BurningRate / PocketVolumeFraction);

        var pocketSurfaceFraction = pocketParams.PocketSurfaceFraction;

        return new MixedComputationParams
        {
            BurningRate = burningRate,
            InterPocketVolumeFraction = InterPocketVolumeFraction,
            PocketVolumeFraction = PocketVolumeFraction,
            InterPocketBurningRateFraction = interPocketBurningRateFraction,
            PocketBurningRateFraction = pocketBurningRateFraction,
            InterPocketComputationParams = interPocketParams,
            PocketComputationParams = pocketParams,
            PocketSurfaceFraction = pocketSurfaceFraction
        };
    }
}
