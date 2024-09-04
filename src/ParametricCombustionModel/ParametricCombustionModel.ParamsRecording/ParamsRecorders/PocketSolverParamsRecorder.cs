using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.ParamsRecording.Interfaces;
using ParametricCombustionModel.ParamsRecording.Models;

namespace ParametricCombustionModel.ParamsRecording.ParamsRecorders;

public class PocketSolverParamsRecorder : PocketPropellantSolver, IParamsRecorder<PocketComputationParams>
{
    public PocketSolverParamsRecorder(double pressure,
                                      PropellantParams propellantParams,
                                      KineticFlameParams pocketSkeletonFlameParams,
                                      KineticFlameParams pocketOutSkeletonFlameParams,
                                      DiffusionFlameParams pocketParams,
                                      MetalCombustionParams metalSkeletonParams)
        : base(pressure,
               propellantParams,
               pocketSkeletonFlameParams,
               pocketOutSkeletonFlameParams,
               pocketParams,
               metalSkeletonParams)
    {
    }

    public PocketComputationParams GetRecord(Span<double> surfaceTemperatures, ref CombustionSolverParams burnParams)
    {
        const int surfaceTemperatureOffset = 1;

        var decompositionRate = GetDecomposeRate(surfaceTemperatures[surfaceTemperatureOffset], ref burnParams);
        var burningRate = GetBurnRate(decompositionRate, ref PropellantParams);
        var skeletonKineticFlameHeatFlow = SkeletonHelper.GetKineticFlameHeatFlow(Pressure,
                                                                                  surfaceTemperatures
                                                                                      [surfaceTemperatureOffset],
                                                                                  decompositionRate,
                                                                                  ref burnParams);
        var skeletonKineticFlameHeight = GetKineticFlameHeight(ref PocketSkeletonFlameParams,
                                                               surfaceTemperatures[surfaceTemperatureOffset],
                                                               skeletonKineticFlameHeatFlow);
        var outSkeletonKineticFlameHeatFlow = OutSkeletonHelper.GetKineticFlameHeatFlow(Pressure,
                                                                                        surfaceTemperatures
                                                                                            [surfaceTemperatureOffset],
                                                                                        decompositionRate,
                                                                                        ref burnParams);
        var outSkeletonKineticFlameHeight = GetKineticFlameHeight(ref PocketOutSkeletonFlameParams,
                                                                  surfaceTemperatures[surfaceTemperatureOffset],
                                                                  outSkeletonKineticFlameHeatFlow);
        var diffusionFlameHeatFlow = GetDiffusionFlameHeatFlow(surfaceTemperatures[surfaceTemperatureOffset],
                                                               GetDiffusionFlameHeight(decompositionRate,
                                                                                       ref burnParams));
        var metalBurningHeatFlow = GetMetalBurningHeatFlow(GetAverageMetalBurningTemperature(), ref burnParams);

        return new PocketComputationParams
        {
            SurfaceTemperature = surfaceTemperatures[surfaceTemperatureOffset],
            BurningRate = burningRate,
            DecomposingRate = decompositionRate,
            SkeletonKineticFlameHeatFlow = skeletonKineticFlameHeatFlow,
            SkeletonKineticFlameHeight = skeletonKineticFlameHeight,
            OutSkeletonKineticFlameHeatFlow = outSkeletonKineticFlameHeatFlow,
            OutSkeletonKineticFlameHeight = outSkeletonKineticFlameHeight,
            DiffusionFlameHeatFlow = diffusionFlameHeatFlow,
            MetalBurningHeatFlow = metalBurningHeatFlow,
            PocketSurfaceFraction = PropellantParams.SkeletonSurfaceFraction
        };
    }

    private static double GetKineticFlameHeight(ref KineticFlameParams kineticFlameParams,
                                                double surfaceTemperature,
                                                double heatFlow)
    {
        return kineticFlameParams.ThermalConductivity * (kineticFlameParams.FinalTemperature - surfaceTemperature) / heatFlow;
    }
}
