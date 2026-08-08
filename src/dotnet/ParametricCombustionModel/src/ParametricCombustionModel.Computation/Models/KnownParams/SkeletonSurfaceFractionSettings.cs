using System.Text.Json;
using System.Text.Json.Serialization;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

/// <summary>How <c>f_s</c>, the share of the pocket surface covered by the skeleton layer, is obtained.
/// </summary>
public enum SkeletonSurfaceFractionMode
{
    /// <summary>
    /// The historical closure: a per-propellant polynomial in pressure, divided by the pocket mass
    /// fraction. The coefficients are a fit to measured aluminium-agglomeration data and carry no
    /// mechanism; there are 22 of them across the five shipped compositions and none transfers to a
    /// composition that was not measured. This is the default, so a run with no configuration computes
    /// what it always computed.
    /// </summary>
    Polynomial,

    /// <summary>
    /// Skeleton coverage as the share of the pocket's carbon that equilibrium leaves condensed. See
    /// <see cref="SkeletonSurfaceFractionSettings"/> for the derivation and
    /// <c>generate_skeleton_carbon_equilibrium.py</c> for the calculation that produces the table.
    /// </summary>
    EquilibriumCarbon
}

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
    /// <para>This is what <see cref="SkeletonSurfaceFractionMode.Polynomial"/> produces, and it is why
    /// the solver has one code path rather than two: the historical closure is the special case of a
    /// coverage that happens not to vary with temperature, so an unconfigured run computes exactly what
    /// it always computed, bit for bit.</para>
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

/// <summary>
/// Equilibrium skeleton coverage for the whole propellant set, keyed by propellant name and pressure.
///
/// <para>Produced by <c>generate_skeleton_carbon_equilibrium.py</c> and read from
/// <c>data/skeleton_carbon_equilibrium.json</c>. It is derived data — a pure function of the recipe and
/// of the thermodynamics, with no dependence on the optimisation vector — which is exactly why it is
/// computed once ahead of a run instead of inside the fitness function, where a Gibbs minimisation per
/// bisection step would be ruinous.</para>
/// </summary>
public sealed class SkeletonCarbonEquilibriumTable
{
    private readonly Dictionary<string, Dictionary<long, SkeletonCoverageCurve>> _curves;

    private SkeletonCarbonEquilibriumTable(Dictionary<string, Dictionary<long, SkeletonCoverageCurve>> curves,
                                           string source)
    {
        _curves = curves;
        Source = source;
    }

    /// <summary>Where the table was read from, for the resolved-run record.</summary>
    public string Source { get; }

    /// <summary>Propellant names the table covers, ordered.</summary>
    public IEnumerable<string> PropellantNames => _curves.Keys.Order(StringComparer.Ordinal);

    /// <summary>
    /// The coverage curve for one propellant at one pressure.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The propellant or the pressure is not in the table. Thrown rather than interpolated across
    /// pressures or defaulted to a neighbour: a missing entry means the table was generated from a
    /// different propellants file than the run is using, and silently modelling one composition with
    /// another's chemistry is the failure this closure exists to remove.
    /// </exception>
    public SkeletonCoverageCurve CurveFor(string propellantName, double pressurePascals)
    {
        if (!_curves.TryGetValue(propellantName, out var byPressure))
            throw new InvalidOperationException(
                $"Skeleton coverage table '{Source}' has no entry for propellant '{propellantName}' " +
                $"(it covers: {string.Join(", ", PropellantNames)}). Regenerate it from the propellants " +
                "file this run loads: python generate_skeleton_carbon_equilibrium.py");

        if (!byPressure.TryGetValue(PressureKey(pressurePascals), out var curve))
            throw new InvalidOperationException(
                $"Skeleton coverage table '{Source}' has no entry for propellant '{propellantName}' at " +
                $"{pressurePascals / 1e6:0.###} MPa. The table must carry every pressure frame of the " +
                "propellants file this run loads; regenerate it with " +
                "python generate_skeleton_carbon_equilibrium.py");

        return curve;
    }

    /// <summary>Reads a table from the JSON the generator writes.</summary>
    public static SkeletonCarbonEquilibriumTable Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Skeleton coverage table '{path}' was not found. Mode " +
                $"{nameof(SkeletonSurfaceFractionMode.EquilibriumCarbon)} needs it; generate it with " +
                "python generate_skeleton_carbon_equilibrium.py", path);

        var file = JsonSerializer.Deserialize<TableFile>(
                       File.ReadAllText(path),
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? throw new InvalidOperationException($"Skeleton coverage table '{path}' is empty.");

        var temperatures = file.SurfaceTemperaturesKelvins?.ToArray() ?? [];
        if (temperatures.Length < 2)
            throw new InvalidOperationException(
                $"Skeleton coverage table '{path}' must sample at least two surface temperatures.");

        var curves = new Dictionary<string, Dictionary<long, SkeletonCoverageCurve>>(StringComparer.Ordinal);
        foreach (var propellant in file.Propellants ?? [])
        {
            var byPressure = new Dictionary<long, SkeletonCoverageCurve>();
            foreach (var frame in propellant.Frames ?? [])
            {
                var coverages = frame.Coverages?.ToArray() ?? [];
                if (coverages.Length != temperatures.Length)
                    throw new InvalidOperationException(
                        $"Skeleton coverage table '{path}': propellant '{propellant.Name}' at " +
                        $"{frame.Pressure / 1e6:0.###} MPa has {coverages.Length} coverage samples but the " +
                        $"table declares {temperatures.Length} surface temperatures.");

                foreach (var coverage in coverages)
                    if (!double.IsFinite(coverage) || coverage < 0 || coverage > 1)
                        throw new InvalidOperationException(
                            $"Skeleton coverage table '{path}': propellant '{propellant.Name}' at " +
                            $"{frame.Pressure / 1e6:0.###} MPa has a coverage of {coverage}, which is not a " +
                            "surface fraction in [0, 1].");

                byPressure[PressureKey(frame.Pressure)] = new SkeletonCoverageCurve(temperatures, coverages);
            }

            curves[propellant.Name ?? string.Empty] = byPressure;
        }

        if (curves.Count == 0)
            throw new InvalidOperationException($"Skeleton coverage table '{path}' covers no propellants.");

        return new SkeletonCarbonEquilibriumTable(curves, path);
    }

    // Pressures round-trip through JSON as doubles and are compared against the propellants file's own
    // frame pressures, which came from the same kind of literal. Keying on the rounded pascal keeps the
    // lookup exact without making it depend on bit-level equality of two independently parsed doubles.
    private static long PressureKey(double pressurePascals) => (long)Math.Round(pressurePascals);

    private sealed record TableFile
    {
        [JsonPropertyName("surfaceTemperaturesKelvins")]
        public List<double>? SurfaceTemperaturesKelvins { get; init; }

        [JsonPropertyName("propellants")]
        public List<TablePropellant>? Propellants { get; init; }
    }

    private sealed record TablePropellant
    {
        [JsonPropertyName("name")] public string? Name { get; init; }

        [JsonPropertyName("frames")] public List<TableFrame>? Frames { get; init; }
    }

    private sealed record TableFrame
    {
        [JsonPropertyName("pressure")] public double Pressure { get; init; }

        [JsonPropertyName("coverages")] public List<double>? Coverages { get; init; }
    }
}

/// <summary>
/// Configuration of the skeleton-surface-fraction closure.
///
/// <para><b>What the equilibrium closure says.</b> The skeleton is the pocket's aluminium held together
/// by the carbonaceous residue of binder pyrolysis. <c>f_s</c> is an <em>area</em> fraction, and by
/// Delesse's theorem the area fraction on a random section equals the volume fraction, so it is the
/// volume fraction of the pocket that is still solid at the surface:</para>
///
/// <code>
///   f_s(p, T_s) = φ_Al + φ_C(s)(T_pore, p) ,     T_pore = (T_s + T_m) / 2
/// </code>
///
/// <para><c>φ_Al</c> is fixed by the recipe. <c>φ_C(s)</c> is what equilibrium leaves condensed of the
/// binder's carbon in the pocket matrix — binder, aluminium and the fine AP, the material that actually
/// pyrolyses inside a pocket. Both come from the recipe and the Gibbs minimiser; neither is fitted, and
/// neither uses an agglomeration measurement. <b>There is no scale factor and none is allowed</b> —
/// Delesse fixes the mapping.</para>
///
/// <para>An earlier version normalised by the carbon inventory instead, <c>n_C(s)/n_C,total</c>. That
/// reads as a char yield rather than an area fraction, it overshot the measured agglomeration by
/// 0.7..4.3×, and it discarded the aluminium — whose volume fraction is the one recipe difference
/// between these compositions (0.170 / 0.218 / 0.258, through the fine-AP dilution). With the aluminium
/// in, the band is 0.6..2.1× and the compositions come out in the measured order at both ends. The cost
/// is the pressure trend: <c>φ_Al</c> does not depend on pressure, so it dilutes the one term that
/// does.</para>
///
/// <para><b>Why pressure lowers it.</b> <c>C(s) + 2H₂ → CH₄</c> is the only carbon sink in this system
/// that proceeds with a decrease in mole number, hence the only one pressure drives <em>forward</em>:
/// Boudouard (<c>C + CO₂ → 2CO</c>) and steam gasification (<c>C + H₂O → CO + H₂</c>) both go with
/// Δn = +1 and are suppressed by pressure. Every transport-based candidate for the same trend —
/// convective blow-off, condensed-phase diffusion, gas-phase diffusion, skeleton thickness — produced the
/// opposite sign, because any length of the form (transport coefficient)/(velocity) necessarily shrinks
/// with pressure. The computed tables show the mechanism directly: between 1 and 6.5 MPa the methane mole
/// fraction rises from 0.08..0.39 to 0.24..0.56 while condensed carbon falls by 10..42 %.</para>
///
/// <para><b>Why it is coupled to the surface temperature and not to the fitted Arrhenius pair.</b> The
/// pocket's pore gas sits at the mean of the surface and skeleton-flame temperatures, so the closure
/// follows the model's own surface temperature — the quantity the bisection is already solving for —
/// rather than a measured burn rate or a measured Vieille exponent. It must never read <c>A_d</c>:
/// <c>A_d</c> and <c>E_d</c> slide along an isokinetic line over three decades while <c>ṁ_d</c> stays
/// put, and a closure reading <c>A_d</c> would inherit that non-identifiability. Nothing here reads
/// either.</para>
///
/// <para><b>Loop gain.</b> Coverage rises with pore temperature at about +0.2..0.3 % per Kelvin, which is
/// an order of magnitude gentler than an Arrhenius destruction term would have been, so evaluating the
/// closure inside the surface-temperature bisection does not threaten the bracketing the solver relies
/// on. If the heat-flux error ever loses its single crossing, the existing search reports failure rather
/// than returning a wrong root.</para>
///
/// <para><b>There is nothing here for the optimiser to choose, and that is load-bearing.</b> <c>f_s</c>
/// weighs <c>q_metal + q_k^(S)</c> against <c>q_k^(OS)</c> in the pocket energy balance and appears
/// nowhere else, so a coverage the optimiser were free to move would be algebraically indistinguishable
/// from redistributing between the skeleton and out-skeleton kinetic-flame pre-exponentials — the same
/// degeneracy that makes the skeleton contact factor a fixed constant.</para>
/// </summary>
public sealed record SkeletonSurfaceFractionSettings
{
    /// <summary>
    /// Which closure to use. <see cref="SkeletonSurfaceFractionMode.Polynomial"/> by default — the
    /// historical behaviour, so an unconfigured run is unchanged.
    /// </summary>
    public SkeletonSurfaceFractionMode Mode { get; init; } = SkeletonSurfaceFractionMode.Polynomial;

    /// <summary>
    /// The equilibrium coverage table, required by <see cref="SkeletonSurfaceFractionMode.EquilibriumCarbon"/>
    /// and unused otherwise. Null by default.
    /// </summary>
    public SkeletonCarbonEquilibriumTable? EquilibriumTable { get; init; }

    /// <summary>The historical closure: the shipped polynomials, no table.</summary>
    public static SkeletonSurfaceFractionSettings Default { get; } = new();

    /// <summary>Throws when the settings cannot describe a physical closure.</summary>
    public void Validate()
    {
        if (!Enum.IsDefined(Mode))
            throw new ArgumentOutOfRangeException(
                nameof(Mode), Mode, "Unknown skeleton-surface-fraction mode.");

        if (Mode == SkeletonSurfaceFractionMode.EquilibriumCarbon && EquilibriumTable is null)
            throw new InvalidOperationException(
                $"Skeleton-surface-fraction mode is {nameof(SkeletonSurfaceFractionMode.EquilibriumCarbon)} " +
                "but no coverage table was loaded. Generate it with " +
                "python generate_skeleton_carbon_equilibrium.py and point " +
                "model.skeletonSurfaceFraction.equilibriumTableFile at it.");
    }
}
