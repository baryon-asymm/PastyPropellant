using ParametricCombustionModel.Computation.Extensions;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Core.Models;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Builders;

/// <summary>
/// The <see cref="ProblemContextByUnitsMatrixBuilder"/> class is responsible for constructing a matrix of <see cref="ProblemContextByUnits"/>
/// instances based on a collection of propellants and pressures. This matrix is used to evaluate various combustion scenarios
/// with different propellant and pressure combinations.
/// </summary>
public class ProblemContextByUnitsMatrixBuilder
{
#region Properties

    /// <summary>
    /// Gets the collection of <see cref="Propellant"/> instances used for building the problem context matrix.
    /// </summary>
    public IEnumerable<Propellant> Propellants { get; init; }

    /// <summary>
    /// Gets or sets the collection of <see cref="Pressure"/> instances used for building the problem context matrix.
    /// </summary>
    public IEnumerable<Pressure> Pressures { get; private set; }

#endregion

#region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemContextByUnitsMatrixBuilder"/> class with the specified collection of propellants.
    /// </summary>
    /// <param name="propellants">
    /// The collection of <see cref="Propellant"/> instances.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propellants"/> is <c>null</c>.
    /// </exception>
    private ProblemContextByUnitsMatrixBuilder(
        IEnumerable<Propellant> propellants)
    {
        Propellants = propellants ?? throw new ArgumentNullException(nameof(propellants));
    }

#endregion

#region Publics

    /// <summary>
    /// Sets the collection of pressures for building the problem context matrix.
    /// </summary>
    /// <param name="pressures">
    /// The collection of <see cref="Pressure"/> instances.
    /// </param>
    /// <returns>
    /// The current instance of the <see cref="ProblemContextByUnitsMatrixBuilder"/> class.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pressures"/> is <c>null</c>.
    /// </exception>
    public ProblemContextByUnitsMatrixBuilder ForPressures(
        IEnumerable<Pressure> pressures)
    {
        Pressures = pressures ?? throw new ArgumentNullException(nameof(pressures));
        return this;
    }

    /// <summary>
    /// Builds a matrix of <see cref="ProblemContextByUnits"/> based on the configured propellants and pressures.
    /// </summary>
    /// <returns>
    /// A two-dimensional array of <see cref="ProblemContextByUnits"/> representing the matrix of problem contexts.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Pressures"/> is <c>null</c> before building the matrix.
    /// </exception>
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
                    Propellant = propellant,
                    Pressure = pressure,
                    PropellantParamsByUnits = GetPropellantParams(pressure, propellant),
                    InterPocketKineticFlameParamsByUnits = GetInterPocketKineticFlameParams(propellant),
                    PocketSkeletonKineticFlameParamsByUnits = GetPocketSkeletonKineticFlameParams(propellant),
                    PocketOutSkeletonKineticFlameParamsByUnits = GetPocketOutSkeletonKineticFlameParams(propellant),
                    PocketDiffusionFlameParamsByUnits = GetPocketDiffusionFlameParams(propellant),
                    PocketMetalCombustionParamsByUnits = GetPocketMetalCombustionParams(pressure, propellant),
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

    /// <summary>
    /// Extracts the <see cref="PropellantParamsByUnits"/> from the specified pressure and propellant.
    /// </summary>
    /// <param name="pressure">
    /// The <see cref="Pressure"/> instance used in the calculation.
    /// </param>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="PropellantParamsByUnits"/> instance containing the parameters extracted from the propellant.
    /// </returns>
    private PropellantParamsByUnits GetPropellantParams(
        Pressure pressure,
        Propellant propellant)
    {
        return new PropellantParamsByUnits
        {
            AverageOxidizerDiameter = Length.FromMeters(propellant.GetAverageParticlesDiameter()),
            Density = Density.FromKilogramsPerCubicMeter(propellant.Density),
            InitialTemperature = Temperature.FromKelvins(propellant.InitialTemperature),
            SkeletonSurfaceFraction = Ratio.FromDecimalFractions(propellant.GetPocketSurfaceFraction(pressure.Pascals)),
            SpecificHeatCapacity = SpecificEntropy.FromJoulesPerKilogramKelvin(propellant.SpecificHeatCapacity)
        };
    }

    /// <summary>
    /// Extracts the <see cref="KineticFlameParamsByUnits"/> for the inter-pocket region from the specified propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="KineticFlameParamsByUnits"/> instance containing the parameters for the inter-pocket region.
    /// </returns>
    private KineticFlameParamsByUnits GetInterPocketKineticFlameParams(
        Propellant propellant)
    {
        var interPocketGasPhase = propellant.InterPocketGasPhase;
        return new KineticFlameParamsByUnits
        {
            FinalTemperature = Temperature.FromKelvins(interPocketGasPhase.KineticFlameTemperature),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(interPocketGasPhase.AverageMolarMass),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(interPocketGasPhase.Lambda_Gas),
            VolumetricSpecificHeatCapacity =
                SpecificEntropy.FromJoulesPerKilogramKelvin(interPocketGasPhase.SpecificHeatCapacity_Volume)
        };
    }

    /// <summary>
    /// Extracts the <see cref="KineticFlameParamsByUnits"/> for the pocket skeleton region from the specified propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="KineticFlameParamsByUnits"/> instance containing the parameters for the pocket skeleton region.
    /// </returns>
    private KineticFlameParamsByUnits GetPocketSkeletonKineticFlameParams(
        Propellant propellant)
    {
        var pocketSkeletonGasPhase = propellant.PocketGasPhase.SkeletonGasPhase;
        return new KineticFlameParamsByUnits
        {
            FinalTemperature = Temperature.FromKelvins(pocketSkeletonGasPhase.KineticFlameTemperature),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(pocketSkeletonGasPhase.AverageMolarMass),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(pocketSkeletonGasPhase.Lambda_Gas),
            VolumetricSpecificHeatCapacity =
                SpecificEntropy.FromJoulesPerKilogramKelvin(pocketSkeletonGasPhase.SpecificHeatCapacity_Volume)
        };
    }

    /// <summary>
    /// Extracts the <see cref="KineticFlameParamsByUnits"/> for the pocket out skeleton region from the specified propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="KineticFlameParamsByUnits"/> instance containing the parameters for the pocket out skeleton region.
    /// </returns>
    private KineticFlameParamsByUnits GetPocketOutSkeletonKineticFlameParams(
        Propellant propellant)
    {
        var pocketOutSkeletonGasPhase = propellant.PocketGasPhase.OutSkeletonGasPhase;
        return new KineticFlameParamsByUnits
        {
            FinalTemperature = Temperature.FromKelvins(pocketOutSkeletonGasPhase.KineticFlameTemperature),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(pocketOutSkeletonGasPhase.AverageMolarMass),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(pocketOutSkeletonGasPhase.Lambda_Gas),
            VolumetricSpecificHeatCapacity =
                SpecificEntropy.FromJoulesPerKilogramKelvin(pocketOutSkeletonGasPhase.SpecificHeatCapacity_Volume)
        };
    }

    /// <summary>
    /// Extracts the <see cref="DiffusionFlameParamsByUnits"/> for the pocket diffusion flame from the specified propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="DiffusionFlameParamsByUnits"/> instance containing the parameters for the pocket diffusion flame.
    /// </returns>
    private DiffusionFlameParamsByUnits GetPocketDiffusionFlameParams(
        Propellant propellant)
    {
        var pocketGasPhase = propellant.PocketGasPhase;
        return new DiffusionFlameParamsByUnits
        {
            FinalTemperature = Temperature.FromKelvins(pocketGasPhase.DiffusionFlameTemperature),
            AverageMolarMass = MolarMass.FromKilogramsPerMole(pocketGasPhase.AverageMolarMass),
            ThermalConductivity = ThermalConductivity.FromWattsPerMeterKelvin(pocketGasPhase.Lambda_Gas),
            VolumetricSpecificHeatCapacity =
                SpecificEntropy.FromJoulesPerKilogramKelvin(pocketGasPhase.SpecificHeatCapacity_Volume)
        };
    }

    /// <summary>
    /// Extracts the <see cref="MetalCombustionParamsByUnits"/> from the specified pressure and propellant.
    /// </summary>
    /// <param name="pressure">
    /// The <see cref="Pressure"/> instance used in the calculation.
    /// </param>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="MetalCombustionParamsByUnits"/> instance containing the parameters for metal combustion.
    /// </returns>
    private MetalCombustionParamsByUnits GetPocketMetalCombustionParams(
        Pressure pressure,
        Propellant propellant)
    {
        return new MetalCombustionParamsByUnits
        {
            MetalBoilingTemperature = Temperature.FromKelvins(propellant.GetMetalBoilingTemperature(pressure.Pascals)),
            MetalMeltingTemperature = Temperature.FromKelvins(propellant.GetMetalMeltingTemperature())
        };
    }

#endregion

#region Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="ProblemContextByUnitsMatrixBuilder"/> class with the specified collection of propellants.
    /// </summary>
    /// <param name="propellants">
    /// The collection of <see cref="Propellant"/> instances.
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="ProblemContextByUnitsMatrixBuilder"/> class.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propellants"/> is <c>null</c>.
    /// </exception>
    public static ProblemContextByUnitsMatrixBuilder FromPropellants(
        IEnumerable<Propellant> propellants)
    {
        return new ProblemContextByUnitsMatrixBuilder(propellants);
    }

#endregion
}
