using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.Models;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Builders;

public class ProblemContextByUnitsMatrixBuilder
{
#region Properties

    public IEnumerable<Propellant> Propellants { get; init; }
    public IEnumerable<Pressure> Pressures { get; private set; }

#endregion

#region Constructors

    private ProblemContextByUnitsMatrixBuilder(
        IEnumerable<Propellant> propellants)
    {
        Propellants = propellants ?? throw new ArgumentNullException(nameof(propellants));
    }

#endregion

#region Publics

    public ProblemContextByUnitsMatrixBuilder ForPressures(
        IEnumerable<Pressure> pressures)
    {
        Pressures = pressures ?? throw new ArgumentNullException(nameof(pressures));
        return this;
    }

    public ProblemContextByUnits[,] BuildMatrix()
    {
        if (Pressures is null)
            throw new InvalidOperationException("Pressures must be set before building matrix.");

        var matrix = new ProblemContextByUnits[Propellants.Count(), Pressures.Count()];

        using var propellantsIterator = Propellants.GetEnumerator();

        for (var i = 0; propellantsIterator.MoveNext(); i++)
        {
            using var pressuresIterator = Pressures.GetEnumerator();
            for (var j = 0; pressuresIterator.MoveNext(); j++)
            {
                var pressure = pressuresIterator.Current;
                var propellant = propellantsIterator.Current;

                matrix[i, j] = new ProblemContextByUnits
                {
                    Pressure = pressure,
                    PropellantParams = GetPropellantParams(pressure, propellant),
                    InterPocketKineticFlameParams = GetInterPocketKineticFlameParams(propellant),
                    PocketSkeletonKineticFlameParams = GetPocketSkeletonKineticFlameParams(propellant),
                    PocketOutSkeletonKineticFlameParams = GetPocketOutSkeletonKineticFlameParams(propellant),
                    PocketDiffusionFlameParams = GetPocketDiffusionFlameParams(propellant),
                    PocketMetalCombustionParams = GetPocketMetalCombustionParams(pressure, propellant),
                    InterPocketVolumeFraction =
                        Ratio.FromDecimalFractions(propellant.GetInterPocketAreaVolumeFraction()),
                    PocketVolumeFraction =
                        Ratio.FromDecimalFractions(propellant.GetPocketAreaVolumeFraction()),
                    MixedCombustionParams = new MixedCombustionParams(),
                    InterPocketCombustionParams = new InterPocketCombustionParams(),
                    PocketCombustionParams = new PocketCombustionParams()
                };
            }
        }

        return matrix;
    }

#endregion

#region StructParams Extractor Methods From Propellant

    private PropellantParams GetPropellantParams(
        Pressure pressure,
        Propellant propellant)
    {
        return new PropellantParams
        {
            AverageOxidizerDiameter = Length.FromMeters(propellant.GetAverageParticlesDiameter()),
            Density = Density.FromKilogramsPerCubicMeter(propellant.Density),
            InitialTemperature = Temperature.FromKelvins(propellant.InitialTemperature),
            SkeletonSurfaceFraction = Ratio.FromDecimalFractions(propellant.GetPocketSurfaceFraction(pressure.Pascals)),
            SpecificHeatCapacity = SpecificEntropy.FromJoulesPerKilogramKelvin(propellant.SpecificHeatCapacity)
        };
    }

    private KineticFlameParams GetInterPocketKineticFlameParams(
        Propellant propellant)
    {
        var interPocketGasPhase = propellant.InterPocketGasPhase;
        return new KineticFlameParams
        {
            FinalTemperature = Temperature.FromKelvins(interPocketGasPhase.KineticFlameTemperature),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(interPocketGasPhase.AverageMolarMass),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(interPocketGasPhase.Lambda_Gas),
            VolumetricSpecificHeatCapacity =
                SpecificEntropy.FromJoulesPerKilogramKelvin(interPocketGasPhase.SpecificHeatCapacity_Volume)
        };
    }

    private KineticFlameParams GetPocketSkeletonKineticFlameParams(
        Propellant propellant)
    {
        var pocketSkeletonGasPhase = propellant.PocketGasPhase.SkeletonGasPhase;
        return new KineticFlameParams
        {
            FinalTemperature = Temperature.FromKelvins(pocketSkeletonGasPhase.KineticFlameTemperature),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(pocketSkeletonGasPhase.AverageMolarMass),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(pocketSkeletonGasPhase.Lambda_Gas),
            VolumetricSpecificHeatCapacity =
                SpecificEntropy.FromJoulesPerKilogramKelvin(pocketSkeletonGasPhase.SpecificHeatCapacity_Volume)
        };
    }

    private KineticFlameParams GetPocketOutSkeletonKineticFlameParams(
        Propellant propellant)
    {
        var pocketOutSkeletonGasPhase = propellant.PocketGasPhase.OutSkeletonGasPhase;
        return new KineticFlameParams
        {
            FinalTemperature = Temperature.FromKelvins(pocketOutSkeletonGasPhase.KineticFlameTemperature),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(pocketOutSkeletonGasPhase.AverageMolarMass),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(pocketOutSkeletonGasPhase.Lambda_Gas),
            VolumetricSpecificHeatCapacity =
                SpecificEntropy.FromJoulesPerKilogramKelvin(pocketOutSkeletonGasPhase.SpecificHeatCapacity_Volume)
        };
    }

    private DiffusionFlameParams GetPocketDiffusionFlameParams(
        Propellant propellant)
    {
        var pocketGasPhase = propellant.PocketGasPhase;
        return new DiffusionFlameParams
        {
            FinalTemperature = Temperature.FromKelvins(pocketGasPhase.DiffusionFlameTemperature),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(pocketGasPhase.AverageMolarMass),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(pocketGasPhase.Lambda_Gas),
            VolumetricSpecificHeatCapacity =
                SpecificEntropy.FromJoulesPerKilogramKelvin(pocketGasPhase.SpecificHeatCapacity_Volume)
        };
    }

    private MetalCombustionParams GetPocketMetalCombustionParams(
        Pressure pressure,
        Propellant propellant)
    {
        return new MetalCombustionParams
        {
            MetalBoilingTemperature = Temperature.FromKelvins(propellant.GetMetalBoilingTemperature(pressure.Pascals)),
            MetalMeltingTemperature = Temperature.FromKelvins(propellant.GetMetalMeltingTemperature())
        };
    }

#endregion

#region Static Methods

    public static ProblemContextByUnitsMatrixBuilder FromPropellants(
        IEnumerable<Propellant> propellants)
    {
        return new ProblemContextByUnitsMatrixBuilder(propellants);
    }

#endregion
}
