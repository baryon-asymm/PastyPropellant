using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Core.Models.PropellantComponents;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using UnitsNet;

namespace ParametricCombustionModel.PlotRenderer.Tests;

/// <summary>
/// Builds the smallest problem contexts the group renderers will accept. The collection step under test
/// reads only the propellant name, the pressure and the burn rates, so everything else is a benign
/// placeholder; the values are supplied inline rather than loaded from a data file so that the fuel names —
/// which are the whole subject of the skip rule — are stated by the test itself.
/// </summary>
internal static class PlotRendererFixture
{
    /// <summary>A solver that must never run: these tests cover data selection, not solving.</summary>
    private sealed class UnusedSolver : ISolverVisitor
    {
        public void Visit(in CombustionSolverParamsByUnits solverParamsByUnits, ProblemContextByUnits context) =>
            throw new InvalidOperationException("Plot collection must not run the solver.");

        public void Visit(in CombustionSolverParamsByDoubles solverParams, ProblemContextByDoubles context) =>
            throw new InvalidOperationException("Plot collection must not run the solver.");
    }

    private static Propellant MakePropellant(string name) =>
        new(
            Name: name,
            A: 1e-6,
            Nu: 0.5,
            Components: Array.Empty<BaseComponent>(),
            Density: 1700.0,
            SpecificHeatCapacity: 1400.0,
            InitialTemperature: 293.0,
            PocketSurfaceFractionCoefficients: new[] { 0.5, 0.0 },
            ConfidenceIntervals: null,
            PocketMassFraction: 0.5,
            PressureFrames: null);

    /// <summary>A [fuel, pressure] problem with a converged burn rate at every point.</summary>
    public static OptimizationProblemByUnits MakeProblem(
        IReadOnlyList<string> fuelNames,
        IReadOnlyList<double> pascals)
    {
        var matrix = new ProblemContextByUnits[fuelNames.Count, pascals.Count];

        for (var fuel = 0; fuel < fuelNames.Count; fuel++)
        {
            var propellant = MakePropellant(fuelNames[fuel]);
            for (var pressure = 0; pressure < pascals.Count; pressure++)
                matrix[fuel, pressure] = MakeContext(propellant, pascals[pressure]);
        }

        return new OptimizationProblemByUnits(matrix, new UnusedSolver(), Array.Empty<IPenaltyEvaluator>());
    }

    private static ProblemContextByUnits MakeContext(Propellant propellant, double pascals) =>
        new()
        {
            Propellant = propellant,
            Pressure = Pressure.FromPascals(pascals),
            PropellantParamsByUnits = new PropellantParamsByUnits
            {
                AverageOxidizerDiameter = Length.FromMicrometers(100.0),
                Density = Density.FromKilogramsPerCubicMeter(propellant.Density),
                InitialTemperature = Temperature.FromKelvins(propellant.InitialTemperature),
                SkeletonSurfaceFraction = Ratio.FromDecimalFractions(0.5),
                SpecificHeatCapacity =
                    SpecificEntropy.FromJoulesPerKilogramKelvin(propellant.SpecificHeatCapacity)
            },
            InterPocketKineticFlameParamsByUnits = MakeKineticFlameParams(),
            PocketSkeletonKineticFlameParamsByUnits = MakeKineticFlameParams(),
            PocketOutSkeletonKineticFlameParamsByUnits = MakeKineticFlameParams(),
            PocketDiffusionFlameParamsByUnits = new DiffusionFlameParamsByUnits
            {
                FinalTemperature = Temperature.FromKelvins(3000.0),
                AverageMolarMass = MolarMass.FromKilogramsPerMole(0.025),
                ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(0.2),
                VolumetricSpecificHeatCapacity = SpecificEntropy.FromJoulesPerKilogramKelvin(1500.0)
            },
            PocketMetalCombustionParamsByUnits = new MetalCombustionParamsByUnits
            {
                MetalBoilingTemperature = Temperature.FromKelvins(2740.0),
                MetalMeltingTemperature = Temperature.FromKelvins(933.0),
                SkeletonContactFactor = 1.0
            },
            SkeletonLayerParamsByUnits = new SkeletonLayerParamsByUnits
            {
                Porosity = Ratio.FromDecimalFractions(0.3),
                CondensedThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(150.0)
            },
            InterPocketVolumeFraction = Ratio.FromDecimalFractions(0.5),
            PocketVolumeFraction = Ratio.FromDecimalFractions(0.5),
            MixedCombustionParams = new MixedCombustionParams
            {
                BurnRateIsFound = true,
                BurnRate = Speed.FromMillimetersPerSecond(3.0)
            },
            InterPocketCombustionParams = new InterPocketCombustionParams(),
            PocketCombustionParams = new PocketCombustionParams()
        };

    private static KineticFlameParamsByUnits MakeKineticFlameParams() =>
        new()
        {
            FinalTemperature = Temperature.FromKelvins(2500.0),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(0.025),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(0.2),
            VolumetricSpecificHeatCapacity = SpecificEntropy.FromJoulesPerKilogramKelvin(1500.0)
        };
}
