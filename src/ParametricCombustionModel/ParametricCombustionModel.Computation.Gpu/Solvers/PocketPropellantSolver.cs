using ILGPU.Algorithms;
using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Computation.Gpu.Solvers;

public static class PocketPropellantSolver
{
    public static double GetBurningRate(double pressure,
                                        out double surfaceTemperature,
                                        ref CombustionSolverParams burnParams,
                                        ref PropellantParams propellantParams,
                                        ref KineticFlameParams pocketSkeletonFlameParams,
                                        ref KineticFlameParams pocketOutSkeletonFlameParams,
                                        ref DiffusionFlameParams pocketParams,
                                        ref MetalCombustionParams metalParams)
    {
        surfaceTemperature = TryFindSurfaceTemperature(pressure,
                                                       ref burnParams,
                                                       ref propellantParams,
                                                       ref pocketSkeletonFlameParams,
                                                       ref pocketOutSkeletonFlameParams,
                                                       ref pocketParams,
                                                       ref metalParams);

        if (surfaceTemperature < 0)
            return -1;

        var decomposingRate = BasePropellantSolver.GetDecomposingRate(surfaceTemperature,
                                                                      ref burnParams);

        return BasePropellantSolver.GetBurningRate(decomposingRate, ref propellantParams);
    }

    private static double TryFindSurfaceTemperature(double pressure,
                                                    ref CombustionSolverParams burnParams,
                                                    ref PropellantParams propellantParams,
                                                    ref KineticFlameParams pocketSkeletonFlameParams,
                                                    ref KineticFlameParams pocketOutSkeletonFlameParams,
                                                    ref DiffusionFlameParams pocketParams,
                                                    ref MetalCombustionParams metalParams)
    {
        const double minSegmentTemperature = 100;
        const double maxSegmentTemperature = 5000;

        var surfaceTemperature = GetSurfaceTemperatureByBinarySearch(minSegmentTemperature,
                                                                     maxSegmentTemperature,
                                                                     pressure,
                                                                     ref burnParams,
                                                                     ref propellantParams,
                                                                     ref pocketSkeletonFlameParams,
                                                                     ref pocketOutSkeletonFlameParams,
                                                                     ref pocketParams,
                                                                     ref metalParams);

        return surfaceTemperature;
    }

    private static double GetSurfaceTemperatureByBinarySearch(double leftTemperature,
                                                              double rightTemperature,
                                                              double pressure,
                                                              ref CombustionSolverParams burnParams,
                                                              ref PropellantParams propellantParams,
                                                              ref KineticFlameParams pocketSkeletonFlameParams,
                                                              ref KineticFlameParams pocketOutSkeletonFlameParams,
                                                              ref DiffusionFlameParams pocketParams,
                                                              ref MetalCombustionParams metalParams,
                                                              double error = 1e-6)
    {
        var leftValue = GetSurfaceHeatFlowsError(pressure,
                                                 leftTemperature,
                                                 ref burnParams,
                                                 ref propellantParams,
                                                 ref pocketSkeletonFlameParams,
                                                 ref pocketOutSkeletonFlameParams,
                                                 ref pocketParams,
                                                 ref metalParams);
        var rightValue = GetSurfaceHeatFlowsError(pressure,
                                                  rightTemperature,
                                                  ref burnParams,
                                                  ref propellantParams,
                                                  ref pocketSkeletonFlameParams,
                                                  ref pocketOutSkeletonFlameParams,
                                                  ref pocketParams,
                                                  ref metalParams);

        if (leftValue * rightValue > 0)
            return -1;

        var middleTemperature = (leftTemperature + rightTemperature) / 2;
        while (Math.Abs(rightTemperature - leftTemperature) > error)
        {
            var middleValue = GetSurfaceHeatFlowsError(pressure,
                                                       middleTemperature,
                                                       ref burnParams,
                                                       ref propellantParams,
                                                       ref pocketSkeletonFlameParams,
                                                       ref pocketOutSkeletonFlameParams,
                                                       ref pocketParams,
                                                       ref metalParams);

            if (middleValue * leftValue < 0)
            {
                rightTemperature = middleTemperature;
                rightValue = middleValue;
            }
            else
            {
                leftTemperature = middleTemperature;
                leftValue = middleValue;
            }

            middleTemperature = (leftTemperature + rightTemperature) / 2;
        }

        return middleTemperature;
    }

    private static double GetSurfaceHeatFlowsError(double pressure,
                                                   double surfaceTemperature,
                                                   ref CombustionSolverParams burnParams,
                                                   ref PropellantParams propellantParams,
                                                   ref KineticFlameParams pocketSkeletonFlameParams,
                                                   ref KineticFlameParams pocketOutSkeletonFlameParams,
                                                   ref DiffusionFlameParams pocketParams,
                                                   ref MetalCombustionParams metalParams)
    {
        var decomposingRate = BasePropellantSolver.GetDecomposingRate(surfaceTemperature,
                                                                      ref burnParams);
        var outSkeletonKineticFlameHeatFlow = GetOutSkeletonKineticFlameHeatFlow(pressure,
                                                                                 surfaceTemperature,
                                                                                 decomposingRate,
                                                                                 ref burnParams,
                                                                                 ref pocketOutSkeletonFlameParams);
        var skeletonKineticFlameHeatFlow = GetSkeletonKineticFlameHeatFlow(pressure,
                                                                           surfaceTemperature,
                                                                           decomposingRate,
                                                                           ref burnParams,
                                                                           ref pocketSkeletonFlameParams);
        var averageMetalBurningTemperature = GetAverageMetalBurningTemperature(ref metalParams);
        var metalBurningHeatFlow = GetMetalBurningHeatFlow(averageMetalBurningTemperature,
                                                           ref burnParams);
        var diffusionFlameHeight = GetDiffusionFlameHeight(decomposingRate,
                                                           ref burnParams,
                                                           ref propellantParams,
                                                           ref pocketParams);
        var diffusionFlameHeatFlow = GetDiffusionFlameHeatFlow(surfaceTemperature,
                                                               diffusionFlameHeight,
                                                               ref pocketParams);

        var skeletonKineticHeatFlow = (1 - propellantParams.SkeletonSurfaceFraction) * outSkeletonKineticFlameHeatFlow;
        var outsideSkeletonHeatFlow = propellantParams.SkeletonSurfaceFraction *
                                      (metalBurningHeatFlow + skeletonKineticFlameHeatFlow);
        var totalHeatFlow = skeletonKineticHeatFlow + outsideSkeletonHeatFlow + diffusionFlameHeatFlow;

        return totalHeatFlow - decomposingRate * (
            propellantParams.SpecificHeatCapacity
            * (surfaceTemperature - propellantParams.InitialTemperature)
            + burnParams.DeltaH
        );
    }

    private static double GetOutSkeletonKineticFlameHeatFlow(double pressure,
                                                             double surfaceTemperature,
                                                             double decomposingRate,
                                                             ref CombustionSolverParams burnParams,
                                                             ref KineticFlameParams flameParams)
    {
        var A_kinetic_flame = burnParams.AKineticFlamePocketOutSkeleton;
        var E_kinetic_flame = burnParams.EKineticFlamePocketOutSkeleton;
        return GetKineticFlameHeatFlow(pressure,
                                       surfaceTemperature,
                                       decomposingRate,
                                       A_kinetic_flame,
                                       E_kinetic_flame,
                                       ref burnParams,
                                       ref flameParams);
    }

    private static double GetSkeletonKineticFlameHeatFlow(double pressure,
                                                          double surfaceTemperature,
                                                          double decomposingRate,
                                                          ref CombustionSolverParams burnParams,
                                                          ref KineticFlameParams flameParams)
    {
        var A_kinetic_flame = burnParams.AKineticFlamePocketSkeleton;
        var E_kinetic_flame = burnParams.EKineticFlamePocketSkeleton;
        return GetKineticFlameHeatFlow(pressure,
                                       surfaceTemperature,
                                       decomposingRate,
                                       A_kinetic_flame,
                                       E_kinetic_flame,
                                       ref burnParams,
                                       ref flameParams);
    }

    private static double GetKineticFlameHeatFlow(double pressure,
                                                  double surfaceTemperature,
                                                  double decomposingRate,
                                                  double A_kinetic_flame,
                                                  double E_kinetic_flame,
                                                  ref CombustionSolverParams burnParams,
                                                  ref KineticFlameParams flameParams)
    {
        var lambda_Gas = flameParams.ThermalConductivity;
        var kineticFlameTemperature = flameParams.FinalTemperature;

        var averageKineticFlameTemperature = GetAverageKineticFlameTemperature(surfaceTemperature,
                                                                               ref flameParams);
        var averageMolarDensityKineticFlame = GetAverageMolarDensityKineticFlame(pressure,
                                                                                 averageKineticFlameTemperature,
                                                                                 ref flameParams);
        var kineticFlameHeight = GetKineticFlameHeight(decomposingRate,
                                                       averageKineticFlameTemperature,
                                                       averageMolarDensityKineticFlame,
                                                       A_kinetic_flame,
                                                       E_kinetic_flame,
                                                       ref burnParams);

        return lambda_Gas * (kineticFlameTemperature - surfaceTemperature) / kineticFlameHeight;
    }

    private static double GetAverageKineticFlameTemperature(double surfaceTemperature,
                                                            ref KineticFlameParams flameParams)
    {
        var kineticFlameTemperature = flameParams.FinalTemperature;

        return (surfaceTemperature + kineticFlameTemperature) / 2;
    }

    private static double GetAverageMolarDensityKineticFlame(double pressure,
                                                             double averageKineticFlameTemperature,
                                                             ref KineticFlameParams flameParams)
    {
        var averageMolarMass = flameParams.AverageMolarMass;
        const double gasConstant_R = 8.31;

        return pressure * averageMolarMass / (gasConstant_R * averageKineticFlameTemperature);
    }

    private static double GetKineticFlameHeight(double decomposingRate,
                                                double averageKineticFlameTemperature,
                                                double averageMolarDensityKineticFlame,
                                                double A_kinetic_flame,
                                                double E_kinetic_flame,
                                                ref CombustionSolverParams burnParams)
    {
        var nu = burnParams.Nu;

        const double gasConstant_R = 8.31;

        return decomposingRate / (
            A_kinetic_flame
            * XMath.Exp(-E_kinetic_flame / (gasConstant_R * averageKineticFlameTemperature))
            * XMath.Pow(averageMolarDensityKineticFlame, nu)
        );
    }

    private static double GetAverageMetalBurningTemperature(ref MetalCombustionParams metalParams)
    {
        var metalMeltingTemperature = metalParams.MetalMeltingTemperature;
        var metalBoilingTemperature = metalParams.MetalBoilingTemperature;

        return (metalMeltingTemperature + metalBoilingTemperature) / 2;
    }

    private static double GetMetalBurningHeatFlow(double averageMetalBurningTemperature,
                                                  ref CombustionSolverParams burnParams)
    {
        var H_metal_burning = burnParams.HMetalBurning;
        var E_metal_burning = burnParams.EMetalBurning;
        const double gasConstant_R = 8.31;

        return H_metal_burning
               * Math.Exp(-E_metal_burning / (gasConstant_R * averageMetalBurningTemperature))
               * averageMetalBurningTemperature;
    }

    private static double GetDiffusionFlameHeight(double decomposingRate,
                                                  ref CombustionSolverParams burnParams,
                                                  ref PropellantParams propellantParams,
                                                  ref DiffusionFlameParams pocketParams)
    {
        var averageOxidizerDiameter = propellantParams.AverageOxidizerDiameter;
        var specificHeatCapacity_Volume = pocketParams.VolumetricSpecificHeatCapacity;
        var lambda_Gas = pocketParams.ThermalConductivity;

        return burnParams.KDiffusionHeight
               * specificHeatCapacity_Volume
               * decomposingRate
               * averageOxidizerDiameter
               * averageOxidizerDiameter
               / lambda_Gas;
    }

    private static double GetDiffusionFlameHeatFlow(double surfaceTemperature,
                                                    double diffusionFlameHeight,
                                                    ref DiffusionFlameParams pocketParams)
    {
        var lambda_Gas = pocketParams.ThermalConductivity;
        var diffusionFlameTemperature = pocketParams.FinalTemperature;

        return lambda_Gas * (diffusionFlameTemperature - surfaceTemperature) / diffusionFlameHeight;
    }
}
