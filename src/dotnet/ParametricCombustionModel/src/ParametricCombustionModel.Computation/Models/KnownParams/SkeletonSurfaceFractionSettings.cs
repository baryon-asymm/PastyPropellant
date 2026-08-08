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
    EquilibriumCarbon,

    /// <summary>
    /// Skeleton coverage as the steady state of a rate competition — accumulation of condensed residue
    /// against burnout of the carbonaceous binder residue that holds it. See
    /// <see cref="KineticCoverageSettings"/> for the derivation, the calibration and its limits.
    /// </summary>
    KineticCoverage
}

/// <summary>
/// The kinetic-coverage closure: <c>f_s</c> as the steady state of a two-channel rate competition.
///
/// <para><b>The balance.</b> The pocket surface gains skeleton where condensed residue accumulates and
/// loses it where the carbonaceous binder residue holding the skeleton together is oxidised away. With
/// <c>k_g</c> the accumulation rate and <c>k_d</c> the burnout rate, a coverage balance
/// <c>k_g(1 − f_s) = k_d f_s</c> gives</para>
///
/// <code>
///   f_s = 1 / (1 + K) ,   K = k_d / k_g = a₀ + a₁ · w_fine · (p / p_ref)^m
/// </code>
///
/// <para>where <c>w_fine</c> is the mass fraction of <em>fine</em> ammonium perchlorate — the oxidiser
/// that sits inside the pocket, the coarse fraction being the wall that bounds it.</para>
///
/// <para><b>Why this shape and not another.</b> Three things fix it, and each was tested rather than
/// assumed. (1) Writing both rates per unit skeleton area makes the layer thickness <c>δ</c> cancel
/// identically, so the closure cannot inherit the <c>δ → 0</c> non-identifiability that makes the
/// skeleton Arrhenius pair unreportable, and it is independent of the contact factor. (2) The measured
/// coverage of the all-coarse composition <em>exceeds</em> its whole condensed inventory
/// (0.529 against φ_Al + φ_C = 0.386), so coverage cannot be an inventory-limited quantity; it must be a
/// competition of rates, and in that competition the inventory cancels because both channels scale with
/// the same arriving flux. Normalising <c>K</c> by <c>φ_C</c> or <c>φ_C·φ_s</c> was tried and is
/// <em>worse</em> — leave-one-composition-out error rises from 13..21 % to 19..42 % and the fine-oxidiser
/// coefficient turns negative. (3) No law of the form <c>K ∝ p^a / r^b</c> can work at all: Bas_1 and
/// Bas_4 share ν = 0.70 yet need d ln K / d ln p of 0.609 and 0.015, so the pocket's own recipe must
/// enter, which is what <c>w_fine</c> does.</para>
///
/// <para><b>What the two channels mean.</b> <c>a₀</c> is burnout by the binder's own decomposition
/// products — the binder here is itself a perchlorate, so every pocket has an oxidising channel whether
/// or not fine AP is present — and it carries no pressure dependence, which is why the composition with
/// no fine AP in its pocket is measured flat (coverage 0.529 → 0.522 over 1..6.5 MPa) instead of
/// decaying. <c>a₁ · w_fine · p^m</c> is burnout by the fine AP inside the pocket. The fitted
/// <c>m ≈ 0.7..1</c> was not imposed; it is the order a heterogeneous carbon oxidation first order in
/// oxidiser partial pressure would have.</para>
///
/// <para><b>Calibration, and its limits — read this before quoting a number from a run.</b> The defaults
/// were fitted on <c>Bas_2</c>, <c>Bas_3</c> and <c>Bas_4</c> only: one binder, the same 0.5 % activated
/// carbon, differing solely in AP dispersity, so every input is published. In-sample error on <c>f_s</c>
/// is 4.7..8.6 % RMS; leaving one composition out of the fit and predicting it gives 12.7..20.9 % RMS.
/// These are <b>three calibrated constants, not derived ones</b> — the honest claim is three shared and
/// transferable constants in place of twelve per-composition polynomial coefficients that transfer to
/// nothing, not that the closure is free of fitted numbers the way the equilibrium closure is.</para>
///
/// <para><b>It does not describe Bas_0 or Bas_1</b>, and must not be quoted for them: they carry a
/// different binder (Bas_1's modifier is a ferrocene-containing compound of unpublished loading), and the
/// same constants overshoot them by 13..137 %. Their binder channel is genuinely different — at equal
/// fine-AP loading the decay exponent falls from 0.470 (plain binder) to 0.209 (modified binder plus
/// carbon) — so a per-binder <c>a₀</c> is the physically honest extension, not a fudge; it is deliberately
/// not offered here until there is data to fix it.</para>
///
/// <para><b>Coverage does not depend on the surface temperature under this closure</b>, so the curve it
/// produces is constant and the surface-temperature bisection sees no extra loop gain at all.</para>
/// </summary>
public sealed record KineticCoverageSettings
{
    /// <summary>
    /// <c>a₀</c> — the pressure-independent burnout channel carried by the binder's own decomposition
    /// products. Default from the Bas_2/3/4 calibration; it is fixed almost entirely by the composition
    /// whose pocket holds no fine AP.
    /// </summary>
    public double BinderChannel { get; init; } = 0.9966;

    /// <summary>
    /// <c>a₁</c> — the burnout channel carried by the fine oxidiser inside the pocket, per unit fine-AP
    /// mass fraction at the reference pressure. Default from the Bas_2/3/4 calibration.
    /// </summary>
    public double FineOxidiserChannel { get; init; } = 3.3653;

    /// <summary>
    /// <c>m</c> — the pressure order of the fine-oxidiser channel. Default from the Bas_2/3/4
    /// calibration; the value it takes there is the order a first-order heterogeneous oxidation has.
    /// </summary>
    public double PressureOrder { get; init; } = 0.71;

    /// <summary>
    /// <c>p_ref</c>, in pascals — the pressure at which <see cref="FineOxidiserChannel"/> is quoted. It is
    /// a unit choice, not a physical constant, and moving it rescales <c>a₁</c> by <c>(p_ref/p_ref')^m</c>;
    /// it is configurable only so a recalibration can report <c>a₁</c> at its own reference.
    /// </summary>
    public double ReferencePressurePascals { get; init; } = 1e6;

    /// <summary>The calibrated defaults.</summary>
    public static KineticCoverageSettings Default { get; } = new();

    /// <summary>
    /// Skeleton coverage of the pocket surface.
    /// </summary>
    /// <param name="fineOxidiserMassFraction">
    /// Mass fraction of fine oxidiser in the propellant — the share that lies inside the pockets.
    /// </param>
    /// <param name="pressurePascals">Chamber pressure, Pa.</param>
    public double CoverageAt(double fineOxidiserMassFraction, double pressurePascals)
    {
        var burnout = BinderChannel
                      + FineOxidiserChannel
                      * fineOxidiserMassFraction
                      * Math.Pow(pressurePascals / ReferencePressurePascals, PressureOrder);

        return 1.0 / (1.0 + burnout);
    }

    /// <summary>Throws when the constants cannot describe a coverage in (0, 1].</summary>
    public void Validate()
    {
        Require(BinderChannel, nameof(BinderChannel), "binderChannel");
        Require(FineOxidiserChannel, nameof(FineOxidiserChannel), "fineOxidiserChannel");

        if (!double.IsFinite(PressureOrder))
            throw new ArgumentOutOfRangeException(
                nameof(PressureOrder), PressureOrder,
                "model.skeletonSurfaceFraction.kinetic.pressureOrder must be a finite number.");

        if (!double.IsFinite(ReferencePressurePascals) || ReferencePressurePascals <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(ReferencePressurePascals), ReferencePressurePascals,
                "model.skeletonSurfaceFraction.kinetic.referencePressurePascals must be positive.");

        // Both channels vanishing means f_s ≡ 1 — the pocket is entirely skeleton at every pressure,
        // which is not a closure but the absence of one.
        if (BinderChannel == 0 && FineOxidiserChannel == 0)
            throw new ArgumentOutOfRangeException(
                nameof(BinderChannel), BinderChannel,
                "model.skeletonSurfaceFraction.kinetic has both burnout channels at zero, which pins the " +
                "coverage at 1 for every composition and every pressure.");

        return;

        static void Require(double value, string name, string key)
        {
            if (!double.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(
                    name, value,
                    $"model.skeletonSurfaceFraction.kinetic.{key} must be finite and non-negative; a " +
                    "negative burnout channel is a coverage the surface creates out of nothing.");
        }
    }
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

    /// <summary>
    /// Constants of the <see cref="SkeletonSurfaceFractionMode.KineticCoverage"/> closure, unused by the
    /// other modes. Never null, so the mode can be switched on without also supplying a block.
    /// </summary>
    public KineticCoverageSettings Kinetic { get; init; } = KineticCoverageSettings.Default;

    /// <summary>The historical closure: the shipped polynomials, no table.</summary>
    public static SkeletonSurfaceFractionSettings Default { get; } = new();

    /// <summary>Throws when the settings cannot describe a physical closure.</summary>
    public void Validate()
    {
        if (!Enum.IsDefined(Mode))
            throw new ArgumentOutOfRangeException(
                nameof(Mode), Mode, "Unknown skeleton-surface-fraction mode.");

        // Validated whatever the mode: a broken kinetic block must not lie dormant until someone
        // switches to it, the same rule the DE strategy sub-records follow.
        Kinetic.Validate();

        if (Mode == SkeletonSurfaceFractionMode.EquilibriumCarbon && EquilibriumTable is null)
            throw new InvalidOperationException(
                $"Skeleton-surface-fraction mode is {nameof(SkeletonSurfaceFractionMode.EquilibriumCarbon)} " +
                "but no coverage table was loaded. Generate it with " +
                "python generate_skeleton_carbon_equilibrium.py and point " +
                "model.skeletonSurfaceFraction.equilibriumTableFile at it.");
    }
}
