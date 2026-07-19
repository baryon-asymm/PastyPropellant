namespace ParametricCombustionModel.Computation.Models.ProblemContexts;

/// <summary>
/// The single source of truth for the surface-temperature bracket used by the binary search that solves
/// the transcendental surface-temperature equation of the condensed phase.
/// <para>
/// These bounds are shared by <see cref="ProblemContextByDoubles"/> and <see cref="ProblemContextByUnits"/>,
/// which must always agree: the two context types are the double-based and UnitsNet-based views of the same
/// physical problem, and the parity tests compare their results directly. Previously each type carried its
/// own literal initializer, so the two could silently drift apart.
/// </para>
/// <para>
/// PROVENANCE — these are deliberate experimental settings of the current line of work, not handbook
/// constants and not fitted parameters. They bracket the physically admissible surface temperature; the
/// search reports failure if no root is found inside them, so they act as a soft constraint on the model.
/// The bracket was previously centred near 750 K; the current 600-900 K window is the setting in use on
/// this branch.
/// </para>
/// <para>
/// Widening or narrowing this window can change which root the binary search converges to, and therefore
/// changes computed results. Do not adjust these values without the model owner's approval.
/// </para>
/// </summary>
public static class SurfaceTemperatureSearchBounds
{
    /// <summary>
    /// The lower bound of the surface-temperature search, in Kelvins.
    /// </summary>
    public const double MinKelvins = 600;

    /// <summary>
    /// The upper bound of the surface-temperature search, in Kelvins.
    /// </summary>
    public const double MaxKelvins = 900;
}
