using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.ParamsRecording.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;

namespace ParametricCombustionModel.ParamsRecording.ParamsRecorders;

public class InterPocketSolverParamsRecorder : InterPocketPropellantSolver,
                                               IParamsRecorder<InterPocketComputationParams>
{
    public InterPocketSolverParamsRecorder(double pressure,
                                           PropellantParams propellantParams,
                                           KineticFlameParams flameParams)
        : base(pressure,
               propellantParams,
               flameParams)
    {
    }

    public InterPocketComputationParams GetRecord(Span<double> surfaceTemperatures, ref CombustionSolverParams burnParams)
    {
        const int surfaceTemperatureOffset = 0;

        var decomposingRate = GetDecomposeRate(surfaceTemperatures[surfaceTemperatureOffset], ref burnParams);
        var kineticFlameHeatFlow = GetKineticFlameHeatFlux(Pressure,
                                                           surfaceTemperatures[surfaceTemperatureOffset],
                                                           decomposingRate,
                                                           ref burnParams,
                                                           ref PropellantParams,
                                                           ref FlameParams);
        var kineticFlameHeight = GetKineticFlameHeight(ref FlameParams,
                                                       surfaceTemperatures[surfaceTemperatureOffset],
                                                       kineticFlameHeatFlow);
        var burningRate = GetBurnRate(decomposingRate, ref PropellantParams);

        return new InterPocketComputationParams
        {
            SurfaceTemperature = surfaceTemperatures[surfaceTemperatureOffset],
            BurningRate = burningRate,
            DecomposingRate = decomposingRate,
            KineticFlameHeatFlow = kineticFlameHeatFlow,
            KineticFlameHeight = kineticFlameHeight
        };
    }

    private static double GetKineticFlameHeight(ref KineticFlameParams kineticFlameParams,
                                                double surfaceTemperature,
                                                double heatFlow)
    {
        return kineticFlameParams.ThermalConductivity * (kineticFlameParams.FinalTemperature - surfaceTemperature) / heatFlow;
    }
}
