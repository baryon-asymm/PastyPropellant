using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Computation.Gpu.Solvers;

public static class MixedPropellantSolver
{
    public static double GetBurningRate(double interPocketVolumeFraction,
                                        double pocketVolumeFraction,
                                        double pressure,
                                        ref CombustionSolverParams burnParams,
                                        ref PropellantParams propellantParams,
                                        ref KineticFlameParams interPocketFlameParams,
                                        ref KineticFlameParams pocketSkeletonFlameParams,
                                        ref KineticFlameParams pocketOutSkeletonFlameParams,
                                        ref DiffusionFlameParams pocketParams,
                                        ref MetalCombustionParams metalParams,
                                        out double interPocketSurfaceTemperature,
                                        out double pocketSurfaceTemperature)
    {
        var interPocketPropellantBurningRate =
            InterPocketPropellantSolver.GetBurningRate(pressure,
                                                       ref propellantParams,
                                                       ref interPocketFlameParams,
                                                       ref burnParams,
                                                       out interPocketSurfaceTemperature);

        var pocketPropellantBurningRate =
            PocketPropellantSolver.GetBurningRate(pressure,
                                                  out pocketSurfaceTemperature,
                                                  ref burnParams,
                                                  ref propellantParams,
                                                  ref pocketSkeletonFlameParams,
                                                  ref pocketOutSkeletonFlameParams,
                                                  ref pocketParams,
                                                  ref metalParams);

        if (interPocketPropellantBurningRate <= 0
            || pocketPropellantBurningRate <= 0)
            return -1;

        return GetBurningRate(interPocketVolumeFraction,
                              pocketVolumeFraction,
                              interPocketPropellantBurningRate,
                              pocketPropellantBurningRate);
    }

    private static double GetBurningRate(double interPocketVolumeFraction,
                                         double pocketVolumeFraction,
                                         double interPocketPropellantBurningRate,
                                         double pocketPropellantBurningRate)
    {
        return 1 / (
            interPocketVolumeFraction / interPocketPropellantBurningRate
            + pocketVolumeFraction / pocketPropellantBurningRate
        );
    }
}
