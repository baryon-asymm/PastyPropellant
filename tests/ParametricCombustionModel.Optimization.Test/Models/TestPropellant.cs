using ParametricCombustionModel.Computation.Models.ComputationsParams;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.DTOs;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Core.Models.GasPhases;
using ParametricCombustionModel.Core.Models.PropellantComponents;

namespace ParametricCombustionModel.Optimization.Test.Models;

public static class TestPropellant
{
    public static Propellant Propellant { get; } = new(
        Name: "Bas_1",
        A: 9.000448e-7,
        Nu: 0.7,
        Density: 1800,
        SpecificHeatCapacity: 1500,
        InitialTemperature: 298,
        Components:
        [
            new CombustibleBinder(0.21),
            new AmmoniumPerchlorate(0.3, 1950, 0.35, 0.000204),
            new Aluminum(0.2073),
            new Octogen(0.2827, 1905)
        ],
        BasePocketSurfaceFraction: 0.14,
        PocketMassFraction: 0.7,
        InterPocketGasPhase: new HomogeneousGasPhase
        (
            Lambda_Gas: 0.1,
            AverageMolarMass: 32.7e-3,
            KineticFlameTemperature: 3707,
            SpecificHeatCapacity_Volume: 635.6
        ),
        PocketGasPhase: new HeterogeneousGasPhase
        (
            Lambda_Gas: 0.1,
            AverageMolarMass: 32.7e-3,
            SpecificHeatCapacity_Volume: 635.6,
            DiffusionFlameTemperature: 3666,
            new HomogeneousGasPhase
            (
                Lambda_Gas: 0.1,
                AverageMolarMass: 32.7e-3,
                KineticFlameTemperature: 1591,
                SpecificHeatCapacity_Volume: 635.6
            ),
            new HomogeneousGasPhase
            (
                Lambda_Gas: 0.1,
                AverageMolarMass: 32.7e-3,
                KineticFlameTemperature: 2365,
                SpecificHeatCapacity_Volume: 635.6
            )
        )
    );

    public static OptimizationContext GetOptimizationContext()
    {
        var pressures = new double[]
        {
            1.2e6,
            1.9e6,
            2.7e6,
            3.4e6,
            4.1e6,
            4.9e6,
            5.6e6,
            6.3e6,
            7.1e6,
            7.8e6
        };
        var initialPoint = new double[]
        {
            162697311.56548402,
            84020.81131867878,
            213546547.42051902,
            78200.33588223261,
            213546547.42051902,
            78200.33588223261,
            1.3856,
            154613633.09360924,
            293254.46800615167,
            298.2484335705692,
            4.978658778109116
        };
        var lowerBounds = new double[] { 1e1, 1e1, 1e1, 5e4, 1e1, 5e4, 1.3856, 1e2, 1e2, 1e1, 1e-6 };
        var upperBounds = new double[] { 1e10, 1e7, 1e10, 2e5, 1e10, 2e5, 1.3856, 1e10, 1e7, 1e10, 3 };
        Propellant[] propellants = [Propellant];

        return new OptimizationContext(pressures,
                                       initialPoint,
                                       1000,
                                       100,
                                       lowerBounds,
                                       upperBounds,
                                       599,
                                       751,
                                       propellants);
    }

    public static PropellantParams GetPropellantParams() =>
        new PropellantParams
        {
            AverageOxidizerDiameter = Propellant.GetAverageParticlesDiameter(),
            Density = Propellant.Density,
            InitialTemperature = Propellant.InitialTemperature,
            SpecificHeatCapacity = Propellant.SpecificHeatCapacity,
            PocketSurfaceFraction = Propellant.GetPocketSurfaceFraction(),
        };

    public static KineticThermodynamicParams GetInterPocketThermodynamicParams() =>
        new KineticThermodynamicParams
        {
            AverageMolarMass = Propellant.InterPocketGasPhase.AverageMolarMass,
            KineticFlameTemperature = Propellant.InterPocketGasPhase.KineticFlameTemperature,
            Lambda_Gas = Propellant.InterPocketGasPhase.Lambda_Gas,
            SpecificHeatCapacity_Volume = Propellant.InterPocketGasPhase.SpecificHeatCapacity_Volume,
        };
}
