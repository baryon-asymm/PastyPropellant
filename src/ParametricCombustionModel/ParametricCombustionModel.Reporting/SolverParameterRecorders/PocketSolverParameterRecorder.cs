using ParametricCombustionModel.Computation.BurningPropellantSolvers;
using ParametricCombustionModel.Computation.Models.ComputationsParams;
using ParametricCombustionModel.Reporting.Interfaces;
using ParametricCombustionModel.Reporting.Models.RecordParameters;

namespace ParametricCombustionModel.Reporting.SolverParameterRecorders;

public class PocketSolverParameterRecorder : PocketPropellantSolver, IRecorder<PocketComputationParams>
{
    public PocketSolverParameterRecorder(double pressure,
                                         PropellantParams propellantParams,
                                         KineticThermodynamicParams pocketSkeletonParams,
                                         KineticThermodynamicParams pocketOutSkeletonParams,
                                         HeterogeneousThermodynamicParams pocketParams,
                                         MetalThermodynamicParams metalSkeletonParams)
        : base(pressure,
               propellantParams,
               pocketSkeletonParams,
               pocketOutSkeletonParams,
               pocketParams,
               metalSkeletonParams)
    {
    }

    public PocketComputationParams GetRecord(Span<double> surfaceTemperatures, ref BurningParams burningParams)
    {
        int surfaceTemperatureOffset = 1;

        var decomposingRate = GetDecomposingRate(surfaceTemperatures[surfaceTemperatureOffset], ref burningParams);
        var burningRate = GetBurningRate(decomposingRate, ref PropellantParams);
        var skeletonKineticFlameHeatFlow = GetKineticFlameHeatFlow(Pressure,
                                                                   surfaceTemperatures[surfaceTemperatureOffset],
                                                                   decomposingRate,
                                                                   ref burningParams,
                                                                   ref PropellantParams,
                                                                   ref PocketSkeletonParams);
        var outSkeletonKineticFlameHeatFlow = GetKineticFlameHeatFlow(Pressure,
                                                                      surfaceTemperatures[surfaceTemperatureOffset],
                                                                      decomposingRate,
                                                                      ref burningParams,
                                                                      ref PropellantParams,
                                                                      ref PocketOutSkeletonParams);
        var diffusionFlameHeatFlow = GetDiffusionFlameHeatFlow(surfaceTemperatures[surfaceTemperatureOffset],
                                                               GetDiffusionFlameHeight(decomposingRate, ref burningParams));
        var metalBurningHeatFlow = GetMetalBurningHeatFlow(GetAverageMetalBurningTemperature(), ref burningParams);

        return new PocketComputationParams
        {
            SurfaceTemperature = surfaceTemperatures[surfaceTemperatureOffset],
            BurningRate = burningRate,
            DecomposingRate = decomposingRate,
            SkeletonKineticFlameHeatFlow = skeletonKineticFlameHeatFlow,
            OutSkeletonKineticFlameHeatFlow = outSkeletonKineticFlameHeatFlow,
            DiffusionFlameHeatFlow = diffusionFlameHeatFlow,
            MetalBurningHeatFlow = metalBurningHeatFlow
        };
    }
}
