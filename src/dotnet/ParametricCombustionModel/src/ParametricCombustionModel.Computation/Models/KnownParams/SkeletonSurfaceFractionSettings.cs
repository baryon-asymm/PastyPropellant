namespace ParametricCombustionModel.Computation.Models.KnownParams;

/// <summary>
/// Skeleton coverage of one propellant at one pressure, as a function of the surface temperature the
/// solver is bisecting on.
///
/// <para>Coverage is sampled on the surface-temperature bracket and interpolated linearly between the
/// samples; the underlying equilibrium curve is smooth over 600..900 K, so the sampling error is far
/// below anything the model can resolve. Outside the bracket the end values are held, which the solver
/// never needs — the bisection cannot leave its own bounds — but which keeps the function total.</para>
/// </summary>
public sealed class SkeletonCoverageCurve
{
    private readonly double[] _surfaceTemperaturesKelvins;
    private readonly double[] _coverages;

    internal SkeletonCoverageCurve(double[] surfaceTemperaturesKelvins, double[] coverages)
    {
        _surfaceTemperaturesKelvins = surfaceTemperaturesKelvins;
        _coverages = coverages;
    }

    /// <summary>
    /// A curve that ignores the surface temperature.
    ///
    /// <para>This is what the historical per-propellant polynomial produces, and it is why the solver has
    /// one code path rather than two: that closure is the special case of a coverage which happens not to
    /// vary with temperature, so an unconfigured run computes exactly what it always computed, bit for
    /// bit.</para>
    /// </summary>
    public static SkeletonCoverageCurve Constant(double coverage) =>
        new([0.0, 1.0], [coverage, coverage]);

    /// <summary>Skeleton coverage at the given surface temperature, in Kelvins.</summary>
    public double At(double surfaceTemperatureKelvins)
    {
        var temperatures = _surfaceTemperaturesKelvins;

        if (surfaceTemperatureKelvins <= temperatures[0])
            return _coverages[0];

        for (var i = 1; i < temperatures.Length; i++)
        {
            if (surfaceTemperatureKelvins > temperatures[i])
                continue;

            var span = temperatures[i] - temperatures[i - 1];
            var weight = (surfaceTemperatureKelvins - temperatures[i - 1]) / span;

            return _coverages[i - 1] + weight * (_coverages[i] - _coverages[i - 1]);
        }

        return _coverages[^1];
    }
}
