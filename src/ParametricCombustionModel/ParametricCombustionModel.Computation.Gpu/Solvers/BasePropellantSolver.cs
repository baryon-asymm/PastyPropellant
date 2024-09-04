using ILGPU.Algorithms;
using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Computation.Gpu.Solvers;

public static class BasePropellantSolver
{
    public static double GetBurningRate(double decomposingRate, ref PropellantParams propellantParams)
    {
        return decomposingRate / propellantParams.Density;
    }

    public static double GetKineticFlameHeatFlow(double pressure,
                                                 double surfaceTemperature,
                                                 double decomposingRate,
                                                 ref CombustionSolverParams burnParams,
                                                 ref PropellantParams propellantParams,
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
                                                       ref burnParams);

        return lambda_Gas * (kineticFlameTemperature - surfaceTemperature) / kineticFlameHeight;
    }

    public static double GetKineticFlameHeight(double decomposingRate,
                                               double averageKineticFlameTemperature,
                                               double averageMolarDensityKineticFlame,
                                               ref CombustionSolverParams burnParams)
    {
        var A_kinetic_flame = burnParams.AKineticFlameInterPocket;
        var E_kinetic_flame = burnParams.EKineticFlameInterPocket;
        var nu = burnParams.Nu;

        const double gasConstant_R = 8.31;

        return decomposingRate / (
            A_kinetic_flame
            * XMath.Exp(-E_kinetic_flame / (gasConstant_R * averageKineticFlameTemperature))
            * XMath.Pow(averageMolarDensityKineticFlame, nu)
        );
    }

    public static double GetDecomposingRate(double surfaceTemperature,
                                            ref CombustionSolverParams burnParams)
    {
        var A_decompose = burnParams.ADecompose;
        var E_decompose = burnParams.EDecompose;
        const double gasConstant_R = 8.31;

        return A_decompose * XMath.Exp(-E_decompose / (gasConstant_R * surfaceTemperature));
    }

    public static double GetAverageMolarDensityKineticFlame(double pressure,
                                                            double averageKineticFlameTemperature,
                                                            ref KineticFlameParams flameParams)
    {
        var averageMolarMass = flameParams.AverageMolarMass;
        const double gasConstant_R = 8.31;

        return pressure * averageMolarMass / (gasConstant_R * averageKineticFlameTemperature);
    }

    public static double GetAverageKineticFlameTemperature(double surfaceTemperature,
                                                           ref KineticFlameParams flameParams)
    {
        var kineticFlameTemperature = flameParams.FinalTemperature;

        return (surfaceTemperature + kineticFlameTemperature) / 2;
    }
}
