using ParametricCombustionModel.Computation.Extensions;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Core.Models;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Builders;

/// <summary>
/// The <see cref="ProblemContextByDoublesMatrixBuilder"/> class is responsible for constructing a matrix of <see cref="ProblemContextByDoubles"/>
/// instances based on a collection of propellants and pressures. This matrix is used to evaluate various combustion scenarios
/// with different propellant and pressure combinations.
/// </summary>
public class ProblemContextByDoublesMatrixBuilder
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
    /// Initializes a new instance of the <see cref="ProblemContextByDoublesMatrixBuilder"/> class with the specified collection of propellants.
    /// </summary>
    /// <param name="propellants">
    /// The collection of <see cref="Propellant"/> instances.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propellants"/> is <c>null</c>.
    /// </exception>
    private ProblemContextByDoublesMatrixBuilder(
        IEnumerable<Propellant> propellants)
    {
        ArgumentNullException.ThrowIfNull(propellants, nameof(propellants));
        
        Propellants = propellants;
        Pressures = [];
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
    /// The current instance of the <see cref="ProblemContextByDoublesMatrixBuilder"/> class.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pressures"/> is <c>null</c>.
    /// </exception>
    public ProblemContextByDoublesMatrixBuilder ForPressures(
        IEnumerable<Pressure> pressures)
    {
        Pressures = pressures ?? throw new ArgumentNullException(nameof(pressures));
        return this;
    }

    /// <summary>
    /// Builds a matrix of <see cref="ProblemContextByDoubles"/> based on the configured propellants and pressures.
    /// </summary>
    /// <returns>
    /// A two-dimensional array of <see cref="ProblemContextByDoubles"/> representing the matrix of problem contexts.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Pressures"/> is <c>null</c> before building the matrix.
    /// </exception>
    public ProblemContextByDoubles[,] BuildMatrix()
    {
        if (Pressures is null)
            throw new InvalidOperationException("Pressures must be set before building matrix.");

        var matrix = new ProblemContextByDoubles[Propellants.Count(), Pressures.Count()];

        using var propellantsIterator = Propellants.GetEnumerator();

        for (var i = 0; propellantsIterator.MoveNext(); i++)
        {
            using var pressuresIterator = Pressures.GetEnumerator();
            for (var j = 0; pressuresIterator.MoveNext(); j++)
            {
                var pressure = pressuresIterator.Current;
                var propellant = propellantsIterator.Current;

                matrix[i, j] = new ProblemContextByDoubles
                {
                    Propellant = propellant,
                    Pressure = pressure.Pascals,
                    PropellantParams = GetPropellantParams(pressure, propellant),
                    InterPocketKineticFlameParams = GetInterPocketKineticFlameParams(propellant),
                    PocketSkeletonKineticFlameParams = GetPocketSkeletonKineticFlameParams(propellant),
                    PocketOutSkeletonKineticFlameParams = GetPocketOutSkeletonKineticFlameParams(propellant),
                    PocketDiffusionFlameParams = GetPocketDiffusionFlameParams(propellant),
                    PocketMetalCombustionParams = GetPocketMetalCombustionParams(pressure, propellant),
                    InterPocketVolumeFraction = propellant.GetInterPocketAreaVolumeFraction(),
                    PocketVolumeFraction = propellant.GetPocketAreaVolumeFraction(),
                    MixedCombustionParams = new MixedCombustionParamsByDoubles(),
                    InterPocketCombustionParams = new InterPocketCombustionParamsByDoubles(),
                    PocketCombustionParams = new PocketCombustionParamsByDoubles()
                };
            }
        }

        return matrix;
    }

#endregion

#region StructParams Extractor Methods From Propellant

    /// <summary>
    /// Extracts the <see cref="PropellantParamsByDoubles"/> from the specified pressure and propellant.
    /// </summary>
    /// <param name="pressure">
    /// The <see cref="Pressure"/> instance used in the calculation.
    /// </param>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="PropellantParamsByDoubles"/> instance containing the parameters extracted from the propellant.
    /// </returns>
    private PropellantParamsByDoubles GetPropellantParams(
        Pressure pressure,
        Propellant propellant)
    {
        return new PropellantParamsByDoubles
        {
            AverageOxidizerDiameter = propellant.GetAverageParticlesDiameter(),
            Density = propellant.Density,
            InitialTemperature = propellant.InitialTemperature,
            SkeletonSurfaceFraction = propellant.GetPocketSurfaceFraction(pressure.Pascals),
            SpecificHeatCapacity = propellant.SpecificHeatCapacity
        };
    }

    /// <summary>
    /// Extracts the <see cref="KineticFlameParamsByDoubles"/> for the inter-pocket region from the specified propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="KineticFlameParamsByDoubles"/> instance containing the parameters for the inter-pocket region.
    /// </returns>
    private KineticFlameParamsByDoubles GetInterPocketKineticFlameParams(
        Propellant propellant)
    {
        var interPocketGasPhase = propellant.InterPocketGasPhase;
        return new KineticFlameParamsByDoubles
        {
            FinalTemperature = interPocketGasPhase.KineticFlameTemperature,
            AverageMolarMass = interPocketGasPhase.AverageMolarMass,
            ThermalConductivity = interPocketGasPhase.Lambda_Gas,
            VolumetricSpecificHeatCapacity = interPocketGasPhase.SpecificHeatCapacity_Volume
        };
    }

    /// <summary>
    /// Extracts the <see cref="KineticFlameParamsByDoubles"/> for the pocket skeleton region from the specified propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="KineticFlameParamsByDoubles"/> instance containing the parameters for the pocket skeleton region.
    /// </returns>
    private KineticFlameParamsByDoubles GetPocketSkeletonKineticFlameParams(
        Propellant propellant)
    {
        var pocketSkeletonGasPhase = propellant.PocketGasPhase.SkeletonGasPhase;
        return new KineticFlameParamsByDoubles
        {
            FinalTemperature = pocketSkeletonGasPhase.KineticFlameTemperature,
            AverageMolarMass = pocketSkeletonGasPhase.AverageMolarMass,
            ThermalConductivity = pocketSkeletonGasPhase.Lambda_Gas,
            VolumetricSpecificHeatCapacity = pocketSkeletonGasPhase.SpecificHeatCapacity_Volume
        };
    }

    /// <summary>
    /// Extracts the <see cref="KineticFlameParamsByDoubles"/> for the pocket out skeleton region from the specified propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="KineticFlameParamsByDoubles"/> instance containing the parameters for the pocket out skeleton region.
    /// </returns>
    private KineticFlameParamsByDoubles GetPocketOutSkeletonKineticFlameParams(
        Propellant propellant)
    {
        var pocketOutSkeletonGasPhase = propellant.PocketGasPhase.OutSkeletonGasPhase;
        return new KineticFlameParamsByDoubles
        {
            FinalTemperature = pocketOutSkeletonGasPhase.KineticFlameTemperature,
            AverageMolarMass = pocketOutSkeletonGasPhase.AverageMolarMass,
            ThermalConductivity = pocketOutSkeletonGasPhase.Lambda_Gas,
            VolumetricSpecificHeatCapacity = pocketOutSkeletonGasPhase.SpecificHeatCapacity_Volume
        };
    }

    /// <summary>
    /// Extracts the <see cref="DiffusionFlameParamsByDoubles"/> for the pocket diffusion flame from the specified propellant.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="DiffusionFlameParamsByDoubles"/> instance containing the parameters for the pocket diffusion flame.
    /// </returns>
    private DiffusionFlameParamsByDoubles GetPocketDiffusionFlameParams(
        Propellant propellant)
    {
        var pocketGasPhase = propellant.PocketGasPhase;
        return new DiffusionFlameParamsByDoubles
        {
            FinalTemperature = pocketGasPhase.DiffusionFlameTemperature,
            AverageMolarMass = pocketGasPhase.AverageMolarMass,
            ThermalConductivity = pocketGasPhase.Lambda_Gas,
            VolumetricSpecificHeatCapacity = pocketGasPhase.SpecificHeatCapacity_Volume
        };
    }

    /// <summary>
    /// Extracts the <see cref="MetalCombustionParamsByDoubles"/> from the specified pressure and propellant.
    /// </summary>
    /// <param name="pressure">
    /// The <see cref="Pressure"/> instance used in the calculation.
    /// </param>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance used in the calculation.
    /// </param>
    /// <returns>
    /// A <see cref="MetalCombustionParamsByDoubles"/> instance containing the parameters for metal combustion.
    /// </returns>
    private MetalCombustionParamsByDoubles GetPocketMetalCombustionParams(
        Pressure pressure,
        Propellant propellant)
    {
        return new MetalCombustionParamsByDoubles
        {
            MetalBoilingTemperature = propellant.GetMetalBoilingTemperature(pressure.Pascals),
            MetalMeltingTemperature = propellant.GetMetalMeltingTemperature()
        };
    }

#endregion

#region Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="ProblemContextByDoublesMatrixBuilder"/> class with the specified collection of propellants.
    /// </summary>
    /// <param name="propellants">
    /// The collection of <see cref="Propellant"/> instances.
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="ProblemContextByDoublesMatrixBuilder"/> class.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propellants"/> is <c>null</c>.
    /// </exception>
    public static ProblemContextByDoublesMatrixBuilder FromPropellants(
        IEnumerable<Propellant> propellants)
    {
        return new ProblemContextByDoublesMatrixBuilder(propellants);
    }

#endregion
}
