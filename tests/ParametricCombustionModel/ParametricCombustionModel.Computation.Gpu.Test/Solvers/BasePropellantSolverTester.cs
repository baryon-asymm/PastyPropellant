using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ParametricCombustionModel.Computation.Gpu.Solvers;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Test.Common.Models;

namespace ParametricCombustionModel.Computation.Gpu.Test.Solvers;

public class BasePropellantSolverTester
{
    [Fact]
    public void TestGetBurningRate()
    {
        using var context = Context.Create(builder => builder.Cuda().EnableAlgorithms());
        var device = context.GetPreferredDevice(preferCPU: false);
        using var accelerator = device.CreateAccelerator(context);

        var pressure = 7.8e6;
        var burningParams = DefaultPointsHolder.ExistSolution;
        var propellantParams = DefaultPropellant.PropellantParams;
        var interPocketParams = DefaultPropellant.InterPocketFlameParams;
        var pocketSkeletonParams = DefaultPropellant.PocketSkeletonFlameParams;
        var pocketOutSkeletonParams = DefaultPropellant.PocketOutSkeletonFlameParams;
        var pocketParams = DefaultPropellant.PocketThermodynamicParams;

        var metalParams = new MetalCombustionParams
        {
            MetalBoilingTemperature = DefaultPropellant.Propellant.GetMetalBoilingTemperature(pressure),
            MetalMeltingTemperature = DefaultPropellant.Propellant.GetMetalMeltingTemperature()
        };
        
        var interPocketVolumeFraction = DefaultPropellant.Propellant.GetInterPocketAreaVolumeFraction();
        var pocketVolumeFraction = 1 - interPocketVolumeFraction;

        var testKernel = accelerator.LoadAutoGroupedStreamKernel<
            Index1D,
            double,
            ArrayView<double>,
            ArrayView<CombustionSolverParams>,
            double,
            double,
            PropellantParams,
            KineticFlameParams,
            KineticFlameParams,
            KineticFlameParams,
            DiffusionFlameParams,
            MetalCombustionParams>(TestKernelForBinarySearch);

        const int taskCount = 100_000_000;
        using var array = accelerator.Allocate1D<double>(taskCount);

        var burningParamsArray = new CombustionSolverParams[taskCount];
        for (var i = 0; i < taskCount; i++)
            burningParamsArray[i] = burningParams;

        using var burningParamsArrayView = accelerator.Allocate1D(burningParamsArray);

        testKernel((int)array.Length,
                   pressure,
                   array.View,
                   burningParamsArrayView.View,
                   interPocketVolumeFraction,
                   pocketVolumeFraction,
                   propellantParams,
                   interPocketParams,
                   pocketSkeletonParams,
                   pocketOutSkeletonParams,
                   pocketParams,
                   metalParams);

        accelerator.Synchronize();

        var results = array.GetAsArray1D();

        const int precision = 6;
        foreach (var result in results)
            Assert.Equal(61.434488, result * 1000, precision);
    }

    private static void TestKernelForBinarySearch(Index1D index,
                                                  double pressure,
                                                  ArrayView<double> results,
                                                  ArrayView<CombustionSolverParams> burningParamsArray,
                                                  double interPocketVolumeFraction,
                                                  double pocketVolumeFraction,
                                                  PropellantParams propellantParams,
                                                  KineticFlameParams interPocketFlameParams,
                                                  KineticFlameParams pocketSkeletonFlameParams,
                                                  KineticFlameParams pocketOutSkeletonFlameParams,
                                                  DiffusionFlameParams pocketParams,
                                                  MetalCombustionParams metalParams)
    {
        var burningParams = burningParamsArray[index];
        var result = MixedPropellantSolver.GetBurningRate(interPocketVolumeFraction,
                                                          pocketVolumeFraction,
                                                          pressure,
                                                          ref burningParams,
                                                          ref propellantParams,
                                                          ref interPocketFlameParams,
                                                          ref pocketSkeletonFlameParams,
                                                          ref pocketOutSkeletonFlameParams,
                                                          ref pocketParams,
                                                          ref metalParams,
                                                          out _,
                                                          out _);

        results[index] = result;
    }
}
