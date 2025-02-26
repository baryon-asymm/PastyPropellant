using ParametricCombustionModel.Computation.BurningPropellantSolvers;
using ParametricCombustionModel.Computation.Models.ComputationsParams;
using ParametricCombustionModel.Reporting.Interfaces;
using ParametricCombustionModel.Reporting.Models.RecordParameters;

namespace ParametricCombustionModel.Reporting.SolverParameterRecorders;

public class InterPocketSolverParameterRecorder : InterPocketPropellantSolver, IRecorder<InterPocketComputationParams>
{
    public InterPocketSolverParameterRecorder(double pressure,
                                              PropellantParams propellantParams,
                                              KineticThermodynamicParams thermodynamicParams)
        : base(pressure,
               propellantParams,
               thermodynamicParams)
    {
    }

    public InterPocketComputationParams GetRecord(Span<double> surfaceTemperatures, ref BurningParams burningParams)
    {
        int surfaceTemperatureOffset = 0;

        var decomposingRate = GetDecomposingRate(surfaceTemperatures[surfaceTemperatureOffset], ref burningParams);
        var kineticFlameHeatFlow = GetKineticFlameHeatFlow(Pressure,
                                                           surfaceTemperatures[surfaceTemperatureOffset],
                                                           decomposingRate,
                                                           ref burningParams,
                                                           ref PropellantParams,
                                                           ref ThermodynamicParams);
        var burningRate = GetBurningRate(decomposingRate, ref PropellantParams);

        return new InterPocketComputationParams
        {
            SurfaceTemperature = surfaceTemperatures[surfaceTemperatureOffset],
            BurningRate = burningRate,
            DecomposingRate = decomposingRate,
            KineticFlameHeatFlow = kineticFlameHeatFlow
        };
    }
}
