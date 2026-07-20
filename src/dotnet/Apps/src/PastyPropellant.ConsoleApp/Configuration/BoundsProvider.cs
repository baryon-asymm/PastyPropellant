using PastyPropellant.Core.Models;

namespace PastyPropellant.ConsoleApp.Configuration;

/// <summary>
/// Single source of the differential-evolution search box.
///
/// <para>Two bounds vectors exist and only one of them is a free choice. The 18-element <em>base</em>
/// vector (<see cref="GetBaseLowerBound"/> / <see cref="GetBaseUpperBound"/>) is the physical
/// parameter list — one entry per model constant, with its unit in the comment. The 32-element
/// <em>group</em> vector is derived from it mechanically by <see cref="ExpandToGroupVector"/> and is
/// never edited directly: widening a bound is therefore always a one-line change to the base vector,
/// and the three composition blocks cannot drift apart.</para>
///
/// <para>The group layout is the one documented in <c>CLAUDE.md</c> and mirrored by
/// <c>CompositionGroups</c>, <c>GroupCombustionSolverParams.ToCompositionVector</c> and
/// <c>GroupOptimizationResult.CompositionContexts</c>:</para>
/// <list type="bullet">
/// <item><c>[0..10]</c> — the 11 parameters shared across all three compositions</item>
/// <item><c>[11..17]</c> — the 7 specific to the Bas_2 group (which covers Bas_3 and Bas_4)</item>
/// <item><c>[18..24]</c> — the 7 specific to Bas_1</item>
/// <item><c>[25..31]</c> — the 7 specific to Bas_0</item>
/// </list>
///
/// <para>Which base slot feeds which group slot is fixed by <see cref="SharedParameterIndices"/> and
/// <see cref="CompositionParameterIndices"/>. Anything that slices or bounds-checks the 32-vector must
/// follow the same slicing.</para>
/// </summary>
public static class BoundsProvider
{
    /// <summary>Length of the physical (per-composition) parameter vector.</summary>
    public const int BaseVectorLength = 18;

    /// <summary>Number of parameters optimised once across all compositions.</summary>
    public const int SharedParameterCount = 11;

    /// <summary>Number of parameters optimised separately for each composition.</summary>
    public const int CompositionParameterCount = 7;

    /// <summary>
    /// Length of the grouped vector actually handed to the optimiser:
    /// 11 shared + 7 × 3 composition-specific = 32.
    /// </summary>
    public const int GroupVectorLength =
        SharedParameterCount + CompositionParameterCount * CompositionGroups.Count;

    /// <summary>
    /// Names of the 18 base parameters, in base-vector order. These are the keys a run-configuration
    /// file uses to override a single bound by name rather than by position — the whole point being
    /// that "widen ADecompose" cannot silently land on slot 0 of the wrong vector.
    /// </summary>
    public static IReadOnlyList<string> BaseParameterNames { get; } =
    [
        "ADecompose",
        "EDecompose",
        "AKineticFlameInterPocket",
        "EKineticFlameInterPocket",
        "AKineticFlamePocketOutSkeleton",
        "EKineticFlamePocketOutSkeleton",
        "AKineticFlamePocketSkeleton",
        "EKineticFlamePocketSkeleton",
        "NuInterPocket",
        "NuPocketOutSkeleton",
        "NuPocketSkeleton",
        "AMetalBurningConstant",
        "BMetalBurningConstant",
        "DeltaH",
        "KDiffusionHeight",
        "APowOrder",
        "BPowOrder",
        "KCoefficientRadiationTemperature"
    ];

    /// <summary>
    /// Base-vector slots whose value is shared by all three compositions, in the order they occupy
    /// group slots <c>[0..10]</c>.
    /// </summary>
    public static IReadOnlyList<int> SharedParameterIndices { get; } =
        [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17];

    /// <summary>
    /// Base-vector slots that each composition gets its own copy of, in the order they occupy the
    /// 7 slots of each per-composition block.
    /// </summary>
    public static IReadOnlyList<int> CompositionParameterIndices { get; } =
        [0, 1, 6, 7, 10, 11, 12];

    /// <summary>Lower bound of the 18 physical parameters.</summary>
    public static double[] GetBaseLowerBound()
    {
        return [
            1.0,    // [0]  ADecompose                       kg/(m²·s)
            5e4,    // [1]  EDecompose                       J/mol
            1e5,    // [2]  AKineticFlameInterPocket         1/s
            5e4,    // [3]  EKineticFlameInterPocket         J/mol
            1e5,    // [4]  AKineticFlamePocketOutSkeleton   1/s
            5e4,    // [5]  EKineticFlamePocketOutSkeleton   J/mol
            1e5,    // [6]  AKineticFlamePocketSkeleton      1/s
            5e4,    // [7]  EKineticFlamePocketSkeleton      J/mol
            0.0,    // [8]  NuInterPocket
            0.0,    // [9]  NuPocketOutSkeleton
            0.0,    // [10] NuPocketSkeleton
            1e-10,  // [11] AMetalBurningConstant            m²/s
            1e-12,  // [12] BMetalBurningConstant            m³/s²
            -1e7,   // [13] DeltaH                           J/kg
            1e-3,   // [14] KDiffusionHeight
            0.0,    // [15] APowOrder                        (degenerate dimensional const = 1)
            0.0,    // [16] BPowOrder                        (degenerate dimensional const = 2)
            0.0     // [17] KCoefficientRadiationTemperature opened to [0,1] so DE can explore the radiative-temperature closure
        ];
    }

    /// <summary>Upper bound of the 18 physical parameters.</summary>
    public static double[] GetBaseUpperBound()
    {
        return [
            1e13,    // [0]  ADecompose  (user-chosen: jDE reaches the clean basin and avoids the degenerate corner; Bas_0 was pinned at the old 1e9 ceiling, so this can push lower)
            3e5,    // [1]  EDecompose
            1e13,   // [2]  AKineticFlameInterPocket
            2.5e5,  // [3]  EKineticFlameInterPocket
            1e13,   // [4]  AKineticFlamePocketOutSkeleton
            2.5e5,  // [5]  EKineticFlamePocketOutSkeleton
            1e13,   // [6]  AKineticFlamePocketSkeleton
            2.5e5,  // [7]  EKineticFlamePocketSkeleton
            2.5,    // [8]  NuInterPocket
            2.5,    // [9]  NuPocketOutSkeleton
            2.5,    // [10] NuPocketSkeleton
            1e-3,   // [11] AMetalBurningConstant
            1e-5,   // [12] BMetalBurningConstant
            1e7,    // [13] DeltaH
            1e1,    // [14] KDiffusionHeight
            3.0,    // [15] APowOrder
            3.0,    // [16] BPowOrder
            1.0     // [17] KCoefficientRadiationTemperature (was [0,0], now [0,1])
        ];
    }

    /// <summary>Lower bound of the 32-element group vector, derived from <see cref="GetBaseLowerBound"/>.</summary>
    public static double[] GetGroupLowerBound() => ExpandToGroupVector(GetBaseLowerBound());

    /// <summary>Upper bound of the 32-element group vector, derived from <see cref="GetBaseUpperBound"/>.</summary>
    public static double[] GetGroupUpperBound() => ExpandToGroupVector(GetBaseUpperBound());

    /// <summary>
    /// Projects a positional base bound onto a name-keyed map, in <see cref="BaseParameterNames"/>
    /// order. This is the form a configuration file and the resolved-run sidecar both use.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="baseBound"/> is not 18 elements long.</exception>
    public static Dictionary<string, double> ToNamedBound(IReadOnlyList<double> baseBound)
    {
        ArgumentNullException.ThrowIfNull(baseBound);

        if (baseBound.Count != BaseVectorLength)
            throw new ArgumentException(
                $"Base bound must have {BaseVectorLength} elements but has {baseBound.Count}.",
                nameof(baseBound));

        var named = new Dictionary<string, double>(BaseVectorLength, StringComparer.Ordinal);
        for (int i = 0; i < BaseVectorLength; i++)
            named[BaseParameterNames[i]] = baseBound[i];

        return named;
    }

    /// <summary>
    /// Projects a name-keyed bound back onto the positional base vector. Every one of the 18 names
    /// must be present and no unrecognised name may appear — a typo'd parameter name is a
    /// configuration error, not a value to ignore.
    /// </summary>
    /// <exception cref="ArgumentException">A name is missing or unrecognised.</exception>
    public static double[] ToBaseVector(IReadOnlyDictionary<string, double> namedBound)
    {
        ArgumentNullException.ThrowIfNull(namedBound);

        var unknown = namedBound.Keys.Where(key => !BaseParameterNames.Contains(key)).ToList();
        if (unknown.Count > 0)
            throw new ArgumentException(
                $"Unrecognised bound parameter name(s): {string.Join(", ", unknown)}. " +
                $"Valid names are: {string.Join(", ", BaseParameterNames)}.",
                nameof(namedBound));

        var baseBound = new double[BaseVectorLength];
        for (int i = 0; i < BaseVectorLength; i++)
        {
            if (!namedBound.TryGetValue(BaseParameterNames[i], out var value))
                throw new ArgumentException(
                    $"Missing bound for parameter '{BaseParameterNames[i]}'.", nameof(namedBound));

            baseBound[i] = value;
        }

        return baseBound;
    }

    /// <summary>
    /// Projects an 18-element base bound onto the 32-element group layout: the shared slots once,
    /// then the composition-specific slots repeated for each composition block.
    /// </summary>
    /// <param name="baseBound">An 18-element bound vector in base-parameter order.</param>
    /// <exception cref="ArgumentException"><paramref name="baseBound"/> is not 18 elements long.</exception>
    public static double[] ExpandToGroupVector(IReadOnlyList<double> baseBound)
    {
        ArgumentNullException.ThrowIfNull(baseBound);

        if (baseBound.Count != BaseVectorLength)
            throw new ArgumentException(
                $"Base bound must have {BaseVectorLength} elements but has {baseBound.Count}.",
                nameof(baseBound));

        var groupBounds = new double[GroupVectorLength];

        // Shared parameters occupy [0..10].
        for (int i = 0; i < SharedParameterIndices.Count; i++)
            groupBounds[i] = baseBound[SharedParameterIndices[i]];

        // Composition-specific parameters occupy [11..31] as three consecutive 7-slot blocks.
        for (int composition = 0; composition < CompositionGroups.Count; composition++)
        {
            int blockStart = SharedParameterCount + composition * CompositionParameterCount;
            for (int i = 0; i < CompositionParameterIndices.Count; i++)
                groupBounds[blockStart + i] = baseBound[CompositionParameterIndices[i]];
        }

        return groupBounds;
    }
}
