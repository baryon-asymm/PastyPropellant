using ParametricCombustionModel.Computation.BurningPropellantSolvers;
using ParametricCombustionModel.Computation.Models.ComputationsParams;
using ParametricCombustionModel.Reporting.Interfaces;
using ParametricCombustionModel.Reporting.Models.RecordParameters;

namespace ParametricCombustionModel.Reporting.SolverParameterRecorders;

public class MixedSolverParameterRecorder : MixedPropellantSolver, IRecorder<MixedComputationParams>
{
    public MixedSolverParameterRecorder(double pressure,
                                        double interPocketVolumeFraction,
                                        double pocketVolumeFraction,
                                        InterPocketSolverParameterRecorder interPocketRecorder,
                                        PocketSolverParameterRecorder pocketRecorder)
        : base(pressure,
               interPocketVolumeFraction,
               pocketVolumeFraction,
               interPocketRecorder,
               pocketRecorder)
    {
    }

    public MixedComputationParams GetRecord(Span<double> surfaceTemperatures, ref BurningParams burningParams)
    {
        var interPocketRecorder = InterPocketPropellantSolver as InterPocketSolverParameterRecorder;
        var pocketRecorder = PocketParamsPropellantSolver as PocketSolverParameterRecorder;

        var interPocketParams = interPocketRecorder.GetRecord(surfaceTemperatures, ref burningParams);
        var pocketParams = pocketRecorder.GetRecord(surfaceTemperatures, ref burningParams);

        var burningRate = GetBurningRate(interPocketParams.BurningRate, pocketParams.BurningRate);

        var interPocketBurningRateFraction = burningRate / (interPocketParams.BurningRate / InterPocketVolumeFraction);
        var pocketBurningRateFraction = burningRate / (pocketParams.BurningRate / PocketVolumeFraction);

        return new MixedComputationParams
        {
            BurningRate = burningRate,
            InterPocketVolumeFraction = InterPocketVolumeFraction,
            PocketVolumeFraction = PocketVolumeFraction,
            InterPocketBurningRateFraction = interPocketBurningRateFraction,
            PocketBurningRateFraction = pocketBurningRateFraction,
            InterPocketComputationParams = interPocketParams,
            PocketComputationParams = pocketParams
        };
    }
}
