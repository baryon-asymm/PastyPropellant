using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Units;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

/// <summary>
/// Represents the parameters related to the burn process in units.
/// </summary>
public readonly ref struct CombustionSolverParams
{
    /// <summary>
    /// Gets the decomposition rate constant (pre-exponential factor in the Arrhenius equation for calculating the binder decomposition rate).
    /// Measured in kg/(m^2*s).
    /// </summary>
    public required MassFlux ADecompose { get; init; }

    /// <summary>
    /// Gets the activation energy for decomposition (activation energy in the Arrhenius equation for calculating the binder decomposition rate).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EDecompose { get; init; }

    /// <summary>
    /// Gets the frequency factor for the kinetic flame inter-pocket (pre-exponential factor in the Arrhenius equation for calculating the chemical reaction rates in the flame).
    /// Measured in 1/s.
    /// </summary>
    public required Frequency AKineticFlameInterPocket { get; init; }

    /// <summary>
    /// Gets the activation energy for the kinetic flame inter-pocket (activation energy in the Arrhenius equation for calculating the chemical reaction rates in the flame).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EKineticFlameInterPocket { get; init; }

    /// <summary>
    /// Gets the frequency factor for the kinetic flame pocket out skeleton (pre-exponential factor in the Arrhenius equation for calculating the chemical reaction rates in the flame in the pocket but not within the skeleton layer).
    /// Measured in 1/s.
    /// </summary>
    public required Frequency AKineticFlamePocketOutSkeleton { get; init; }

    /// <summary>
    /// Gets the activation energy for the kinetic flame pocket out skeleton (activation energy in the Arrhenius equation for calculating the chemical reaction rates in the flame in the pocket but not within the skeleton layer).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EKineticFlamePocketOutSkeleton { get; init; }

    /// <summary>
    /// Gets the frequency factor for the kinetic flame pocket skeleton (pre-exponential factor in the Arrhenius equation for calculating the chemical reaction rates in the flame in the pocket and within the skeleton layer).
    /// Measured in 1/s.
    /// </summary>
    public required Frequency AKineticFlamePocketSkeleton { get; init; }

    /// <summary>
    /// Gets the activation energy for the kinetic flame pocket skeleton (activation energy in the Arrhenius equation for calculating the chemical reaction rates in the flame in the pocket and within the skeleton layer).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EKineticFlamePocketSkeleton { get; init; }

    /// <summary>
    /// Gets the order of chemical reactions in kinetic flames.
    /// </summary>
    public required double Nu { get; init; }

    /// <summary>
    /// Gets the metal burning coefficient (complex coefficient in the metal burning law within the skeleton layer).
    /// Measured in W/(K*m^2).
    /// </summary>
    public required HMetalBurningCoefficient HMetalBurning { get; init; }

    /// <summary>
    /// Gets the activation energy for metal burning (activation energy in the metal burning law within the skeleton layer).
    /// Measured in J/mol.
    /// </summary>
    public required MolarEnergy EMetalBurning { get; init; }

    /// <summary>
    /// Gets the specific energy change of the binder (difference between the heat of sublimation and the heat of binder decomposition).
    /// Measured in J/kg.
    /// </summary>
    public required SpecificEnergy DeltaH { get; init; }

    /// <summary>
    /// Gets the diffusion height coefficient.
    /// </summary>
    public required double KDiffusionHeight { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CombustionSolverParams FromVector(
        Span<double> vector)
    {
        return new CombustionSolverParams
        {
            ADecompose = MassFlux.FromKilogramsPerSecondPerSquareMeter(vector[0]),
            EDecompose = MolarEnergy.FromJoulesPerMole(vector[1]),
            AKineticFlameInterPocket = Frequency.FromPerSecond(vector[2]),
            EKineticFlameInterPocket = MolarEnergy.FromJoulesPerMole(vector[3]),
            AKineticFlamePocketOutSkeleton = Frequency.FromPerSecond(vector[4]),
            EKineticFlamePocketOutSkeleton = MolarEnergy.FromJoulesPerMole(vector[5]),
            AKineticFlamePocketSkeleton = Frequency.FromPerSecond(vector[6]),
            EKineticFlamePocketSkeleton = MolarEnergy.FromJoulesPerMole(vector[7]),
            Nu = vector[8],
            HMetalBurning = HMetalBurningCoefficient.FromWattsPerKelvinPerSquareMeter(vector[9]),
            EMetalBurning = MolarEnergy.FromJoulesPerMole(vector[10]),
            DeltaH = SpecificEnergy.FromJoulesPerKilogram(vector[11]),
            KDiffusionHeight = vector[12]
        };
    }
}
