using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Computation.Gpu.Solvers;

public static class InterPocketPropellantSolver
{
    public static double GetBurningRate(double pressure,
                                        ref PropellantParams propellantParams,
                                        ref KineticFlameParams flameParams,
                                        ref CombustionSolverParams burnParams,
                                        out double surfaceTemperature)
    {
        surfaceTemperature = TryFindSurfaceTemperature(pressure,
                                                       ref propellantParams,
                                                       ref flameParams,
                                                       ref burnParams);

        if (surfaceTemperature < 0)
            return -1;

        var decomposingRate = BasePropellantSolver.GetDecomposingRate(surfaceTemperature,
                                                                      ref burnParams);

        return BasePropellantSolver.GetBurningRate(decomposingRate, ref propellantParams);
    }

    private static double TryFindSurfaceTemperature(double pressure,
                                                   ref PropellantParams propellantParams,
                                                   ref KineticFlameParams flameParams,
                                                   ref CombustionSolverParams burnParams)
    {
        const double minSegmentTemperature = 100;
        const double maxSegmentTemperature = 5000;

        var surfaceTemperature = GetSurfaceTemperatureByBinarySearch(minSegmentTemperature,
                                                                     maxSegmentTemperature,
                                                                     pressure,
                                                                     ref burnParams,
                                                                     ref propellantParams,
                                                                     ref flameParams);

        return surfaceTemperature;
    }

    private static double GetSurfaceTemperatureByBinarySearch(double leftTemperature,
                                                              double rightTemperature,
                                                              double pressure,
                                                              ref CombustionSolverParams burnParams,
                                                              ref PropellantParams propellantParams,
                                                              ref KineticFlameParams flameParams,
                                                              double error = 1e-6)
    {
        var leftValue = GetSurfaceHeatFlowsError(pressure,
                                                 leftTemperature,
                                                 ref burnParams,
                                                 ref propellantParams,
                                                 ref flameParams);
        var rightValue = GetSurfaceHeatFlowsError(pressure,
                                                  rightTemperature,
                                                  ref burnParams,
                                                  ref propellantParams,
                                                  ref flameParams);

        if (leftValue * rightValue > 0)
            return -1;

        var middleTemperature = (leftTemperature + rightTemperature) / 2;
        while (Math.Abs(rightTemperature - leftTemperature) > error)
        {
            var middleValue = GetSurfaceHeatFlowsError(pressure,
                                                       middleTemperature,
                                                       ref burnParams,
                                                       ref propellantParams,
                                                       ref flameParams);

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
                                                  ref KineticFlameParams flameParams)
    {
        var delta_H = burnParams.DeltaH;
        var specificHeatCapacity = propellantParams.SpecificHeatCapacity;
        var initialTemperature = propellantParams.InitialTemperature;

        var decomposingRate = BasePropellantSolver.GetDecomposingRate(surfaceTemperature,
                                                                      ref burnParams);
        var kineticFlameHeatFlow = BasePropellantSolver.GetKineticFlameHeatFlow(pressure,
                                                                                surfaceTemperature,
                                                                                decomposingRate,
                                                                                ref burnParams,
                                                                                ref propellantParams,
                                                                                ref flameParams);

        return kineticFlameHeatFlow - decomposingRate * (
            specificHeatCapacity
            * (surfaceTemperature - initialTemperature)
            + delta_H
        );
    }
}
