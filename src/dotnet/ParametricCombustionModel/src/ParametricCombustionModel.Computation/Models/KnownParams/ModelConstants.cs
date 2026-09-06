using ParametricCombustionModel.Computation.Extensions;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

/// <summary>
/// Physical constants of the combustion model that are neither fitted by the optimiser nor properties of an
/// individual propellant — they apply to every composition and every pressure in a run.
///
/// <para>They live here, in one object threaded through the context builders, for a single reason:
/// <b>anything that changes every computed number must appear in the run record</b>. Before this type
/// existed, the metal melting temperature was a <c>const</c> in source, so changing it meant editing and
/// rebuilding, and the resulting PDF said nothing about which value produced it — a whole campaign of runs
/// at 2300 K left no trace of that in any report. Passing these values instead of compiling them in is what
/// lets <c>run_configuration.resolved.json</c> and the PDF header state them.</para>
///
/// <para>The defaults reproduce the historical hardcoded behaviour exactly, so a run with no configuration
/// file computes what it always computed.</para>
/// </summary>
public sealed record ModelConstants
{
    /// <summary>
    /// Melting temperature of the metal skeleton, in Kelvins. Uniform across compositions by design — the
    /// model assumes a single metal (aluminium). Default 1300 K, the historical value.
    /// </summary>
    public double MetalMeltingTemperatureKelvins { get; init; } = PropellantExtensions.MetalMeltingTemperatureKelvins;

    /// <summary>
    /// How the skeleton surface fraction is obtained. Defaults to the historical per-propellant
    /// polynomial fit; see <see cref="SkeletonSurfaceFractionSettings"/> for the kinetic alternative and
    /// why its coefficients are configuration rather than search parameters.
    /// </summary>
    public SkeletonSurfaceFractionSettings SkeletonSurfaceFraction { get; init; } =
        SkeletonSurfaceFractionSettings.Default;

    /// <summary>
    /// Lower bound of the surface-temperature bracket the condensed-phase solve bisects on, in Kelvins.
    /// Default <see cref="ProblemContexts.SurfaceTemperatureSearchBounds.MinKelvins"/>.
    /// </summary>
    public double MinSurfaceTemperatureKelvins { get; init; } =
        ProblemContexts.SurfaceTemperatureSearchBounds.MinKelvins;

    /// <summary>
    /// Upper bound of the surface-temperature bracket, in Kelvins. Default
    /// <see cref="ProblemContexts.SurfaceTemperatureSearchBounds.MaxKelvins"/>.
    ///
    /// <para>The bracket is a <b>deliberate experimental setting, not a handbook constant</b>: it acts as a
    /// soft constraint, because the search reports failure when no root lies inside it, and widening it can
    /// move the solve onto a different root. It is configuration for the same reason as the metal melting
    /// temperature — it changes every computed number, so every run must record which bracket produced
    /// it.</para>
    /// </summary>
    public double MaxSurfaceTemperatureKelvins { get; init; } =
        ProblemContexts.SurfaceTemperatureSearchBounds.MaxKelvins;

    /// <summary>The historical hardcoded constants: 1300 K, polynomial coverage, 600..900 K bracket.</summary>
    public static ModelConstants Default { get; } = new();

    /// <summary>Throws when a value cannot describe a physical model.</summary>
    public void Validate()
    {
        if (!double.IsFinite(MinSurfaceTemperatureKelvins) || MinSurfaceTemperatureKelvins <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(MinSurfaceTemperatureKelvins),
                MinSurfaceTemperatureKelvins,
                "Minimum surface temperature must be a positive, finite number of Kelvins.");

        if (!(MaxSurfaceTemperatureKelvins > MinSurfaceTemperatureKelvins))
            throw new ArgumentOutOfRangeException(
                nameof(MaxSurfaceTemperatureKelvins),
                MaxSurfaceTemperatureKelvins,
                $"Maximum surface temperature must exceed the minimum ({MinSurfaceTemperatureKelvins} K); " +
                "the solve bisects between them.");

        if (!double.IsFinite(MetalMeltingTemperatureKelvins) || MetalMeltingTemperatureKelvins <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(MetalMeltingTemperatureKelvins),
                MetalMeltingTemperatureKelvins,
                "Metal melting temperature must be a positive, finite number of Kelvins.");

        SkeletonSurfaceFraction.Validate();
    }
}
