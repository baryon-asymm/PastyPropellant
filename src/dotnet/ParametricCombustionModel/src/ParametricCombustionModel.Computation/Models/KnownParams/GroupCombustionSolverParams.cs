using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Units;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

#region Group Utilization of Double

/// <summary>
/// Represents the extended group parameters for simultaneous optimization of three propellant compositions.
/// Contains 32 parameters: 11 shared + 7 specific for each of 3 compositions (bas_0, bas_1, bas_2).
/// 
/// Structure:
/// [0-10]   = 11 shared parameters
/// [11-17]  = 7 specific parameters for bas_2 (also applied to bas_3, bas_4)
/// [18-24]  = 7 specific parameters for bas_1
/// [25-31]  = 7 specific parameters for bas_0
/// </summary>
public readonly ref struct GroupCombustionSolverParamsByDoubles
{
    // ===== SHARED PARAMETERS (11) =====
    public required double AKineticFlameInterPocket { get; init; }
    public required double EKineticFlameInterPocket { get; init; }
    public required double AKineticFlamePocketOutSkeleton { get; init; }
    public required double EKineticFlamePocketOutSkeleton { get; init; }
    public required double NuInterPocket { get; init; }
    public required double NuPocketOutSkeleton { get; init; }
    public required double DeltaH { get; init; }
    public required double KDiffusionHeight { get; init; }
    public required double APowOrder { get; init; }
    public required double BPowOrder { get; init; }
    public required double KCoefficientRadiationTemperature { get; init; }

    // ===== BAS_2 SPECIFIC PARAMETERS (7) =====
    public required double ADecomposeBas2 { get; init; }
    public required double EDecomposeBas2 { get; init; }
    public required double AKineticFlamePocketSkeletonBas2 { get; init; }
    public required double EKineticFlamePocketSkeletonBas2 { get; init; }
    public required double NuPocketSkeletonBas2 { get; init; }
    public required double AMetalBurningConstantBas2 { get; init; }
    public required double BMetalBurningConstantBas2 { get; init; }

    // ===== BAS_1 SPECIFIC PARAMETERS (7) =====
    public required double ADecomposeBas1 { get; init; }
    public required double EDecomposeBas1 { get; init; }
    public required double AKineticFlamePocketSkeletonBas1 { get; init; }
    public required double EKineticFlamePocketSkeletonBas1 { get; init; }
    public required double NuPocketSkeletonBas1 { get; init; }
    public required double AMetalBurningConstantBas1 { get; init; }
    public required double BMetalBurningConstantBas1 { get; init; }

    // ===== BAS_0 SPECIFIC PARAMETERS (7) =====
    public required double ADecomposeBas0 { get; init; }
    public required double EDecomposeBas0 { get; init; }
    public required double AKineticFlamePocketSkeletonBas0 { get; init; }
    public required double EKineticFlamePocketSkeletonBas0 { get; init; }
    public required double NuPocketSkeletonBas0 { get; init; }
    public required double AMetalBurningConstantBas0 { get; init; }
    public required double BMetalBurningConstantBas0 { get; init; }

    /// <summary>
    /// Creates GroupCombustionSolverParamsByDoubles from a 32-element vector.
    /// Vector layout:
    /// [0-10]   = shared parameters
    /// [11-17]  = bas_2 specific
    /// [18-24]  = bas_1 specific
    /// [25-31]  = bas_0 specific
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GroupCombustionSolverParamsByDoubles FromVector(ReadOnlySpan<double> vector)
    {
        if (vector.Length != 32)
            throw new ArgumentException("Group vector must contain exactly 32 elements", nameof(vector));

        return new GroupCombustionSolverParamsByDoubles
        {
            // Shared parameters [0-10]
            AKineticFlameInterPocket = vector[0],
            EKineticFlameInterPocket = vector[1],
            AKineticFlamePocketOutSkeleton = vector[2],
            EKineticFlamePocketOutSkeleton = vector[3],
            NuInterPocket = vector[4],
            NuPocketOutSkeleton = vector[5],
            DeltaH = vector[6],
            KDiffusionHeight = vector[7],
            APowOrder = vector[8],
            BPowOrder = vector[9],
            KCoefficientRadiationTemperature = vector[10],

            // Bas_2 specific [11-17]
            ADecomposeBas2 = vector[11],
            EDecomposeBas2 = vector[12],
            AKineticFlamePocketSkeletonBas2 = vector[13],
            EKineticFlamePocketSkeletonBas2 = vector[14],
            NuPocketSkeletonBas2 = vector[15],
            AMetalBurningConstantBas2 = vector[16],
            BMetalBurningConstantBas2 = vector[17],

            // Bas_1 specific [18-24]
            ADecomposeBas1 = vector[18],
            EDecomposeBas1 = vector[19],
            AKineticFlamePocketSkeletonBas1 = vector[20],
            EKineticFlamePocketSkeletonBas1 = vector[21],
            NuPocketSkeletonBas1 = vector[22],
            AMetalBurningConstantBas1 = vector[23],
            BMetalBurningConstantBas1 = vector[24],

            // Bas_0 specific [25-31]
            ADecomposeBas0 = vector[25],
            EDecomposeBas0 = vector[26],
            AKineticFlamePocketSkeletonBas0 = vector[27],
            EKineticFlamePocketSkeletonBas0 = vector[28],
            NuPocketSkeletonBas0 = vector[29],
            AMetalBurningConstantBas0 = vector[30],
            BMetalBurningConstantBas0 = vector[31]
        };
    }

    /// <summary>
    /// Converts group vector (32 parameters) to standard composition vector (18 parameters) for the specified composition.
    /// Composition index follows the raw vector layout: 0 = Bas_2+Bas_3+Bas_4 (combined group), 1 = Bas_1, 2 = Bas_0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[] ToCompositionVector(int compositionIndex)
    {
        if (compositionIndex < 0 || compositionIndex > 2)
            throw new ArgumentOutOfRangeException(nameof(compositionIndex), "Composition index must be 0, 1, or 2");

        var result = new double[18];

        // Map composition-specific parameters (order matches the raw vector layout)
        var (aDecompose, eDecompose, aFlame, eFlame, nuPocket, aMetal, bMetal) = compositionIndex switch
        {
            0 => (ADecomposeBas2, EDecomposeBas2, AKineticFlamePocketSkeletonBas2,
                  EKineticFlamePocketSkeletonBas2, NuPocketSkeletonBas2, AMetalBurningConstantBas2, BMetalBurningConstantBas2),
            1 => (ADecomposeBas1, EDecomposeBas1, AKineticFlamePocketSkeletonBas1,
                  EKineticFlamePocketSkeletonBas1, NuPocketSkeletonBas1, AMetalBurningConstantBas1, BMetalBurningConstantBas1),
            2 => (ADecomposeBas0, EDecomposeBas0, AKineticFlamePocketSkeletonBas0,
                  EKineticFlamePocketSkeletonBas0, NuPocketSkeletonBas0, AMetalBurningConstantBas0, BMetalBurningConstantBas0),
            _ => throw new ArgumentOutOfRangeException()
        };

        // Assembly according to the standard 18-parameter vector structure
        result[0] = aDecompose;
        result[1] = eDecompose;
        result[2] = AKineticFlameInterPocket;
        result[3] = EKineticFlameInterPocket;
        result[4] = AKineticFlamePocketOutSkeleton;
        result[5] = EKineticFlamePocketOutSkeleton;
        result[6] = aFlame;
        result[7] = eFlame;
        result[8] = NuInterPocket;
        result[9] = NuPocketOutSkeleton;
        result[10] = nuPocket;
        result[11] = aMetal;
        result[12] = bMetal;
        result[13] = DeltaH;
        result[14] = KDiffusionHeight;
        result[15] = APowOrder;
        result[16] = BPowOrder;
        result[17] = KCoefficientRadiationTemperature;

        return result;
    }
}

#endregion

#region Group Utilization of UnitsNet

/// <summary>
/// Represents the extended group parameters for simultaneous optimization using UnitsNet types.
/// Contains 32 parameters: 11 shared + 7 specific for each of 3 compositions (bas_0, bas_1, bas_2).
/// </summary>
public readonly ref struct GroupCombustionSolverParamsByUnits
{
    // ===== SHARED PARAMETERS (11) =====
    public required Frequency AKineticFlameInterPocket { get; init; }
    public required MolarEnergy EKineticFlameInterPocket { get; init; }
    public required Frequency AKineticFlamePocketOutSkeleton { get; init; }
    public required MolarEnergy EKineticFlamePocketOutSkeleton { get; init; }
    public required double NuInterPocket { get; init; }
    public required double NuPocketOutSkeleton { get; init; }
    public required SpecificEnergy DeltaH { get; init; }
    public required double KDiffusionHeight { get; init; }
    public required double APowOrder { get; init; }
    public required double BPowOrder { get; init; }
    public required double KCoefficientRadiationTemperature { get; init; }

    // ===== BAS_2 SPECIFIC PARAMETERS (7) =====
    public required MassFlux ADecomposeBas2 { get; init; }
    public required MolarEnergy EDecomposeBas2 { get; init; }
    public required Frequency AKineticFlamePocketSkeletonBas2 { get; init; }
    public required MolarEnergy EKineticFlamePocketSkeletonBas2 { get; init; }
    public required double NuPocketSkeletonBas2 { get; init; }
    public required AMetalBurningConstant AMetalBurningConstantBas2 { get; init; }
    public required BMetalBurningConstant BMetalBurningConstantBas2 { get; init; }

    // ===== BAS_1 SPECIFIC PARAMETERS (7) =====
    public required MassFlux ADecomposeBas1 { get; init; }
    public required MolarEnergy EDecomposeBas1 { get; init; }
    public required Frequency AKineticFlamePocketSkeletonBas1 { get; init; }
    public required MolarEnergy EKineticFlamePocketSkeletonBas1 { get; init; }
    public required double NuPocketSkeletonBas1 { get; init; }
    public required AMetalBurningConstant AMetalBurningConstantBas1 { get; init; }
    public required BMetalBurningConstant BMetalBurningConstantBas1 { get; init; }

    // ===== BAS_0 SPECIFIC PARAMETERS (7) =====
    public required MassFlux ADecomposeBas0 { get; init; }
    public required MolarEnergy EDecomposeBas0 { get; init; }
    public required Frequency AKineticFlamePocketSkeletonBas0 { get; init; }
    public required MolarEnergy EKineticFlamePocketSkeletonBas0 { get; init; }
    public required double NuPocketSkeletonBas0 { get; init; }
    public required AMetalBurningConstant AMetalBurningConstantBas0 { get; init; }
    public required BMetalBurningConstant BMetalBurningConstantBas0 { get; init; }

    /// <summary>
    /// Creates GroupCombustionSolverParamsByUnits from a 32-element vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GroupCombustionSolverParamsByUnits FromVector(ReadOnlySpan<double> vector)
    {
        if (vector.Length != 32)
            throw new ArgumentException("Group vector must contain exactly 32 elements", nameof(vector));

        return new GroupCombustionSolverParamsByUnits
        {
            // Shared parameters [0-10]
            AKineticFlameInterPocket = Frequency.FromPerSecond(vector[0]),
            EKineticFlameInterPocket = MolarEnergy.FromJoulesPerMole(vector[1]),
            AKineticFlamePocketOutSkeleton = Frequency.FromPerSecond(vector[2]),
            EKineticFlamePocketOutSkeleton = MolarEnergy.FromJoulesPerMole(vector[3]),
            NuInterPocket = vector[4],
            NuPocketOutSkeleton = vector[5],
            DeltaH = SpecificEnergy.FromJoulesPerKilogram(vector[6]),
            KDiffusionHeight = vector[7],
            APowOrder = vector[8],
            BPowOrder = vector[9],
            KCoefficientRadiationTemperature = vector[10],

            // Bas_2 specific [11-17]
            ADecomposeBas2 = MassFlux.FromKilogramsPerSecondPerSquareMeter(vector[11]),
            EDecomposeBas2 = MolarEnergy.FromJoulesPerMole(vector[12]),
            AKineticFlamePocketSkeletonBas2 = Frequency.FromPerSecond(vector[13]),
            EKineticFlamePocketSkeletonBas2 = MolarEnergy.FromJoulesPerMole(vector[14]),
            NuPocketSkeletonBas2 = vector[15],
            AMetalBurningConstantBas2 = AMetalBurningConstant.FromSquareMetersPerSecond(vector[16]),
            BMetalBurningConstantBas2 = BMetalBurningConstant.FromCubicMetersPerSquareSecond(vector[17]),

            // Bas_1 specific [18-24]
            ADecomposeBas1 = MassFlux.FromKilogramsPerSecondPerSquareMeter(vector[18]),
            EDecomposeBas1 = MolarEnergy.FromJoulesPerMole(vector[19]),
            AKineticFlamePocketSkeletonBas1 = Frequency.FromPerSecond(vector[20]),
            EKineticFlamePocketSkeletonBas1 = MolarEnergy.FromJoulesPerMole(vector[21]),
            NuPocketSkeletonBas1 = vector[22],
            AMetalBurningConstantBas1 = AMetalBurningConstant.FromSquareMetersPerSecond(vector[23]),
            BMetalBurningConstantBas1 = BMetalBurningConstant.FromCubicMetersPerSquareSecond(vector[24]),

            // Bas_0 specific [25-31]
            ADecomposeBas0 = MassFlux.FromKilogramsPerSecondPerSquareMeter(vector[25]),
            EDecomposeBas0 = MolarEnergy.FromJoulesPerMole(vector[26]),
            AKineticFlamePocketSkeletonBas0 = Frequency.FromPerSecond(vector[27]),
            EKineticFlamePocketSkeletonBas0 = MolarEnergy.FromJoulesPerMole(vector[28]),
            NuPocketSkeletonBas0 = vector[29],
            AMetalBurningConstantBas0 = AMetalBurningConstant.FromSquareMetersPerSecond(vector[30]),
            BMetalBurningConstantBas0 = BMetalBurningConstant.FromCubicMetersPerSquareSecond(vector[31])
        };
    }

    /// <summary>
    /// Converts group vector (32 parameters) to standard composition vector (18 parameters) for the specified composition.
    /// Composition index follows the raw vector layout: 0 = Bas_2+Bas_3+Bas_4 (combined group), 1 = Bas_1, 2 = Bas_0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[] ToCompositionVector(int compositionIndex)
    {
        if (compositionIndex < 0 || compositionIndex > 2)
            throw new ArgumentOutOfRangeException(nameof(compositionIndex), "Composition index must be 0, 1, or 2");

        var result = new double[18];

        // Map composition-specific parameters (order matches the raw vector layout)
        var (aDecompose, eDecompose, aFlame, eFlame, nuPocket, aMetal, bMetal) = compositionIndex switch
        {
            0 => (ADecomposeBas2.KilogramsPerSecondPerSquareMeter, EDecomposeBas2.JoulesPerMole,
                  AKineticFlamePocketSkeletonBas2.PerSecond, EKineticFlamePocketSkeletonBas2.JoulesPerMole,
                  NuPocketSkeletonBas2, AMetalBurningConstantBas2.SquareMetersPerSecond, BMetalBurningConstantBas2.CubicMetersPerSquareSecond),
            1 => (ADecomposeBas1.KilogramsPerSecondPerSquareMeter, EDecomposeBas1.JoulesPerMole,
                  AKineticFlamePocketSkeletonBas1.PerSecond, EKineticFlamePocketSkeletonBas1.JoulesPerMole,
                  NuPocketSkeletonBas1, AMetalBurningConstantBas1.SquareMetersPerSecond, BMetalBurningConstantBas1.CubicMetersPerSquareSecond),
            2 => (ADecomposeBas0.KilogramsPerSecondPerSquareMeter, EDecomposeBas0.JoulesPerMole,
                  AKineticFlamePocketSkeletonBas0.PerSecond, EKineticFlamePocketSkeletonBas0.JoulesPerMole,
                  NuPocketSkeletonBas0, AMetalBurningConstantBas0.SquareMetersPerSecond, BMetalBurningConstantBas0.CubicMetersPerSquareSecond),
            _ => throw new ArgumentOutOfRangeException()
        };

        // Assembly according to the standard 18-parameter vector structure
        result[0] = aDecompose;
        result[1] = eDecompose;
        result[2] = AKineticFlameInterPocket.PerSecond;
        result[3] = EKineticFlameInterPocket.JoulesPerMole;
        result[4] = AKineticFlamePocketOutSkeleton.PerSecond;
        result[5] = EKineticFlamePocketOutSkeleton.JoulesPerMole;
        result[6] = aFlame;
        result[7] = eFlame;
        result[8] = NuInterPocket;
        result[9] = NuPocketOutSkeleton;
        result[10] = nuPocket;
        result[11] = aMetal;
        result[12] = bMetal;
        result[13] = DeltaH.JoulesPerKilogram;
        result[14] = KDiffusionHeight;
        result[15] = APowOrder;
        result[16] = BPowOrder;
        result[17] = KCoefficientRadiationTemperature;

        return result;
    }
}

#endregion
