using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Units;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

#region Utilization of Double

/// <summary>
/// Represents the parameters related to the burn process using native double values.
/// </summary>
public readonly ref struct CombustionSolverParamsByDoubles
{
    /// <summary>
    /// Decomposition rate constant (pre-exponential factor).
    /// Measured in kg/(m^2*s).
    /// </summary>
    public required double ADecompose { get; init; }

    /// <summary>
    /// Activation energy for decomposition.
    /// Measured in J/mol.
    /// </summary>
    public required double EDecompose { get; init; }

    /// <summary>
    /// Frequency factor for the kinetic flame inter-pocket.
    /// Measured in 1/s.
    /// </summary>
    public required double AKineticFlameInterPocket { get; init; }

    /// <summary>
    /// Activation energy for the kinetic flame inter-pocket.
    /// Measured in J/mol.
    /// </summary>
    public required double EKineticFlameInterPocket { get; init; }

    /// <summary>
    /// Frequency factor for the kinetic flame pocket out skeleton.
    /// Measured in 1/s.
    /// </summary>
    public required double AKineticFlamePocketOutSkeleton { get; init; }

    /// <summary>
    /// Activation energy for the kinetic flame pocket out skeleton.
    /// Measured in J/mol.
    /// </summary>
    public required double EKineticFlamePocketOutSkeleton { get; init; }

    /// <summary>
    /// Frequency factor for the kinetic flame pocket skeleton.
    /// Measured in 1/s.
    /// </summary>
    public required double AKineticFlamePocketSkeleton { get; init; }

    /// <summary>
    /// Activation energy for the kinetic flame pocket skeleton.
    /// Measured in J/mol.
    /// </summary>
    public required double EKineticFlamePocketSkeleton { get; init; }

    /// <summary>
    /// Order of chemical reactions in kinetic flames at the inter pocket region.
    /// </summary>
    public required double NuInterPocket { get; init; }
    
    public required double NuPocketOutSkeleton { get; init; }
    
    public required double NuPocketSkeleton { get; init; }
    
    public required double AMetalBurningConstant { get; init; }

    public required double BMetalBurningConstant { get; init; }

    /// <summary>
    /// Specific energy change of the binder.
    /// Measured in J/kg.
    /// </summary>
    public required double DeltaH { get; init; }

    /// <summary>
    /// Diffusion height coefficient.
    /// </summary>
    public required double KDiffusionHeight { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CombustionSolverParamsByDoubles FromVector(
        ReadOnlySpan<double> vector)
    {
        return new CombustionSolverParamsByDoubles
        {
            ADecompose = vector[0],
            EDecompose = vector[1],
            AKineticFlameInterPocket = vector[2],
            EKineticFlameInterPocket = vector[3],
            AKineticFlamePocketOutSkeleton = vector[4],
            EKineticFlamePocketOutSkeleton = vector[5],
            AKineticFlamePocketSkeleton = vector[6],
            EKineticFlamePocketSkeleton = vector[7],
            NuInterPocket = vector[8],
            NuPocketOutSkeleton = vector[9],
            NuPocketSkeleton = vector[10],
            AMetalBurningConstant = vector[11],
            BMetalBurningConstant = vector[12],
            DeltaH = vector[13],
            KDiffusionHeight = vector[14]
        };
    }
}

#endregion

#region Utilization of UnitsNet

/// <summary>
/// Represents the parameters related to the burn process in units.
/// </summary>
public readonly ref struct CombustionSolverParamsByUnits
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
    /// Gets the order of chemical reactions in kinetic flames at the inter pocket region.
    /// </summary>
    public required double NuInterPocket { get; init; }
    
    public required double NuPocketOutSkeleton { get; init; }
    
    public required double NuPocketSkeleton { get; init; }

    public required AMetalBurningConstant AMetalBurningConstant { get; init; }

    public required BMetalBurningConstant BMetalBurningConstant { get; init; }

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
    public static CombustionSolverParamsByUnits FromVector(
        ReadOnlySpan<double> vector)
    {
        return new CombustionSolverParamsByUnits
        {
            ADecompose = MassFlux.FromKilogramsPerSecondPerSquareMeter(vector[0]),
            EDecompose = MolarEnergy.FromJoulesPerMole(vector[1]),
            AKineticFlameInterPocket = Frequency.FromPerSecond(vector[2]),
            EKineticFlameInterPocket = MolarEnergy.FromJoulesPerMole(vector[3]),
            AKineticFlamePocketOutSkeleton = Frequency.FromPerSecond(vector[4]),
            EKineticFlamePocketOutSkeleton = MolarEnergy.FromJoulesPerMole(vector[5]),
            AKineticFlamePocketSkeleton = Frequency.FromPerSecond(vector[6]),
            EKineticFlamePocketSkeleton = MolarEnergy.FromJoulesPerMole(vector[7]),
            NuInterPocket = vector[8],
            NuPocketOutSkeleton = vector[9],
            NuPocketSkeleton = vector[10],
            AMetalBurningConstant = AMetalBurningConstant.FromSquareMetersPerSecond(vector[11]),
            BMetalBurningConstant = BMetalBurningConstant.FromCubicMetersPerSquareSecond(vector[12]),
            DeltaH = SpecificEnergy.FromJoulesPerKilogram(vector[13]),
            KDiffusionHeight = vector[14]
        };
    }
}

#endregion
