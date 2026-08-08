using System.Text.Json;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Extensions;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Computation.Tests;

/// <summary>
/// Pins the equilibrium skeleton-coverage closure.
///
/// <para><b>Why these particular properties.</b> <c>f_s</c> weighs <c>q_metal + q_k^(S)</c> against
/// <c>q_k^(OS)</c> in the pocket energy balance and appears nowhere else, so it is interchangeable with a
/// redistribution between the two kinetic-flame pre-exponentials. That makes three things load-bearing:
/// the default must leave every existing run untouched; the two solver tiers must agree on which closure
/// is in force, or the search would minimise a different model from the one the report is computed from;
/// and a table that does not cover the propellant set must fail loudly rather than quietly substituting
/// some other composition's chemistry.</para>
/// </summary>
public class SkeletonSurfaceFractionTests
{
    private const string PropellantJson = @"propellants.01234.json";
    private const string CoverageTableJson = @"skeleton_carbon_equilibrium.json";

    /// <summary>
    /// The default closure is the historical polynomial, bit for bit.
    ///
    /// <para>This is the guarantee that lets the change ship without invalidating the archive: a run whose
    /// configuration does not mention the closure computes exactly what it always computed, so every
    /// recorded result stays reproducible on the new code. The coverage curve is temperature-dependent
    /// now, so the check sweeps the whole surface-temperature bracket rather than one point.</para>
    /// </summary>
    [Fact]
    public void DefaultClosureIsTheHistoricalPolynomialAtEverySurfaceTemperature()
    {
        Assert.Equal(SkeletonSurfaceFractionMode.Polynomial, SkeletonSurfaceFractionSettings.Default.Mode);
        Assert.Null(SkeletonSurfaceFractionSettings.Default.EquilibriumTable);
        Assert.Equal(
            SkeletonSurfaceFractionMode.Polynomial,
            ModelConstants.Default.SkeletonSurfaceFraction.Mode);

        foreach (var propellant in LoadPropellants())
        foreach (var pressurePascals in Pressures(propellant))
        {
            var expected = propellant.GetPocketSurfaceFraction(pressurePascals);
            var curve = propellant.GetSkeletonCoverage(pressurePascals, null);
            var configured = propellant.GetSkeletonCoverage(
                pressurePascals, ModelConstants.Default.SkeletonSurfaceFraction);

            foreach (var surfaceTemperature in new[]
                     {
                         SurfaceTemperatureSearchBounds.MinKelvins, 675.0, 750.0, 825.0,
                         SurfaceTemperatureSearchBounds.MaxKelvins
                     })
            {
                Assert.Equal(expected, curve.At(surfaceTemperature));
                Assert.Equal(expected, configured.At(surfaceTemperature));
            }
        }
    }

    /// <summary>The shipped table covers every propellant at every pressure frame of the shipped set.</summary>
    [Fact]
    public void TheShippedTableCoversTheShippedPropellantSet()
    {
        var settings = EquilibriumSettings();

        foreach (var propellant in LoadPropellants())
        foreach (var pressurePascals in Pressures(propellant))
        {
            var coverage = propellant.GetSkeletonCoverage(pressurePascals, settings)
                .At(SurfaceTemperatureSearchBounds.MinKelvins);

            Assert.InRange(coverage, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Coverage falls with pressure — the trend the closure exists to reproduce, and the one every
    /// transport mechanism got backwards.
    ///
    /// <para><b>The sign is universal, and that is the closure's actual claim.</b> Methanation
    /// (<c>C + 2H₂ → CH₄</c>, Δn = −1) is the only carbon sink here that runs forward under pressure;
    /// Boudouard and steam gasification (Δn = +1) are suppressed by it. So coverage must fall with
    /// pressure for <em>every</em> composition, with no regime split and no exceptions.</para>
    ///
    /// <para><b>Why there is no longer a regime split.</b> An earlier revision put the pore at
    /// <c>(T_flame,skeleton + T_s)/2</c> and had to carve out Bas_3, whose 2623 K skeleton flame pushed its
    /// pores past the carbon maximum and made coverage <em>rise</em> with pressure. That was an artefact
    /// of using a gas-phase temperature for a condensed-phase body. The pore now sits at
    /// <c>(T_s + T_m)/2</c> — the range the skeleton layer actually spans, since the layer ends where the
    /// metal melts — which is one temperature for all compositions, and the carve-out is gone with it.</para>
    ///
    /// <para><b>Why the fall is bounded on both sides.</b> Below: the trend must not vanish. Above: at
    /// 1450–1600 K the equilibrium carbon sits near its maximum, where methanation is thermodynamically
    /// weak, and <c>φ_Al</c> — which does not depend on pressure at all — carries most of the coverage and
    /// dilutes the one term that does. A fall of tens of per cent would mean the table had been built
    /// under the old flame-temperature rule (which gave −11.3 % for Bas_0 and +0.4 % for Bas_3), so the
    /// upper bound is what pins the correction in place.</para>
    ///
    /// <para><b>The shortfall against measurement is a result, not a defect to tune away.</b> The shipped
    /// polynomials fall 24–57 % over the window while the closure falls 3–5 %. Do not weaken either bound
    /// to close that gap — the gap is the finding.</para>
    /// </summary>
    [Fact]
    public void EquilibriumCoverageFallsWithPressureForEveryComposition()
    {
        const double smallestCredibleFall = 0.01;
        const double largestCredibleFall = 0.08;

        var settings = EquilibriumSettings();

        foreach (var propellant in LoadPropellants())
        foreach (var surfaceTemperature in new[] { 600.0, 750.0, 900.0 })
        {
            var pressures = Pressures(propellant).ToArray();
            var first = propellant.GetSkeletonCoverage(pressures[0], settings).At(surfaceTemperature);
            var last = propellant.GetSkeletonCoverage(pressures[^1], settings).At(surfaceTemperature);
            var fall = 1.0 - last / first;
            var where = $"{propellant.Name} at T_s = {surfaceTemperature} K: coverage went from " +
                        $"{first:0.0000} at {pressures[0] / 1e6:0.##} MPa to {last:0.0000} at " +
                        $"{pressures[^1] / 1e6:0.##} MPa (fall {fall:P1})";

            Assert.True(
                fall >= smallestCredibleFall,
                $"{where}; methanation has Δn = −1, so pressure must eat the skeleton by at least " +
                $"{smallestCredibleFall:P0} — a vanished trend means the pore temperature or the " +
                "equilibrium is wrong.");

            Assert.True(
                fall <= largestCredibleFall,
                $"{where}; near the carbon maximum, diluted by a pressure-independent φ_Al, the fall " +
                $"cannot exceed {largestCredibleFall:P0}. A larger one means the table was built with the " +
                "superseded (T_flame,skeleton + T_s)/2 pore rule.");
        }
    }

    /// <summary>
    /// Coverage varies gently with the surface temperature across the search bracket.
    ///
    /// <para>This is the loop-gain check, and it is the property that makes evaluating the closure
    /// <em>inside</em> the surface-temperature bisection safe: the bisection needs the heat-flux error to
    /// keep a single crossing, and a coverage that moved violently with the trial temperature could
    /// destroy that. The bound here is 0.5 % per Kelvin.</para>
    ///
    /// <para>The <b>sign</b> is deliberately not asserted, and neither is <b>monotonicity</b>. With the
    /// pore at <c>(T_s + T_m)/2</c> the whole search bracket maps to 1450–1600 K, which sits on the broad
    /// maximum of the equilibrium carbon curve. Coverage therefore moves by well under a per cent across
    /// the entire bracket, and the ordering of those samples wobbles in the fourth decimal — Bas_3 at
    /// 1.61 MPa runs 0.1951, 0.1953, 0.1952, 0.1948, 0.1939. That wobble is Gibbs-solver noise on a flat
    /// extremum, not structure. An earlier revision did assert monotonicity, but only because the
    /// superseded <c>(T_flame,skeleton + T_s)/2</c> pore rule put the compositions on steep flanks either
    /// side of the peak; pinning it now would pin numerical noise.</para>
    ///
    /// <para>The correction made the bisection ten times safer — the loop gain fell from 0.027 %/K under
    /// the old rule to 0.003–0.009 %/K. The bound below deliberately stays at what the bisection
    /// tolerates rather than being tightened onto today's numbers: it is a safety property, not a change
    /// detector.</para>
    /// </summary>
    [Fact]
    public void EquilibriumCoverageVariesGentlyWithSurfaceTemperature()
    {
        const double maximumRelativeChangePerKelvin = 0.005;
        var settings = EquilibriumSettings();

        foreach (var propellant in LoadPropellants())
        foreach (var pressurePascals in Pressures(propellant))
        {
            var curve = propellant.GetSkeletonCoverage(pressurePascals, settings);
            var samples = new[] { 600.0, 675.0, 750.0, 825.0, 900.0 }.Select(curve.At).ToArray();

            var gain = Math.Abs(Math.Log(samples[^1] / samples[0])) / (900.0 - 600.0);
            Assert.True(
                gain <= maximumRelativeChangePerKelvin,
                $"{propellant.Name} at {pressurePascals / 1e6:0.##} MPa: coverage moves {gain:P3} per Kelvin " +
                $"of surface temperature, above the {maximumRelativeChangePerKelvin:P1} the bisection is " +
                "assumed to tolerate.");
        }
    }

    /// <summary>Interpolation is linear between samples and flat outside the bracket.</summary>
    [Fact]
    public void CoverageInterpolatesLinearlyBetweenSamples()
    {
        var curve = new SkeletonCarbonEquilibriumTableFixture(
            [600.0, 900.0], [0.20, 0.50]).Curve;

        Assert.Equal(0.20, curve.At(600.0));
        Assert.Equal(0.50, curve.At(900.0));
        Assert.Equal(0.35, curve.At(750.0), 12);
        Assert.Equal(0.20, curve.At(400.0));
        Assert.Equal(0.50, curve.At(1200.0));
    }

    /// <summary>
    /// A propellant the table does not cover fails loudly. Defaulting would model that composition with
    /// another one's chemistry and report nothing about it.
    /// </summary>
    [Fact]
    public void APropellantMissingFromTheTableThrows()
    {
        var settings = EquilibriumSettings();
        var propellant = LoadPropellants()[0] with { Name = "Bas_not_in_the_table" };

        var error = Assert.Throws<InvalidOperationException>(
            () => propellant.GetSkeletonCoverage(1e6, settings));

        Assert.Contains("Bas_not_in_the_table", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A pressure the table does not cover throws rather than interpolating across frames.</summary>
    [Fact]
    public void APressureMissingFromTheTableThrows()
    {
        var settings = EquilibriumSettings();

        var error = Assert.Throws<InvalidOperationException>(
            () => LoadPropellants()[0].GetSkeletonCoverage(7.5e6, settings));

        Assert.Contains("7.5 MPa", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Both solver tiers read the same closure. Drift here would let the DE search minimise one model
    /// while the PDF reports another, with nothing failing.
    /// </summary>
    [Fact]
    public void BothContextTiersUseTheConfiguredClosure()
    {
        var propellants = LoadPropellants();
        var constants = new ModelConstants { SkeletonSurfaceFraction = EquilibriumSettings() };

        var byUnits = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants, constants).BuildMatrix();
        var byDoubles = ProblemContextByDoublesMatrixBuilder.FromPropellants(propellants, constants).BuildMatrix();

        for (var propellantIndex = 0; propellantIndex < byUnits.GetLength(0); propellantIndex++)
        for (var pressureIndex = 0; pressureIndex < byUnits.GetLength(1); pressureIndex++)
        foreach (var surfaceTemperature in new[] { 600.0, 750.0, 900.0 })
        {
            var unitsCoverage = byUnits[propellantIndex, pressureIndex]
                .PropellantParamsByUnits.SkeletonCoverage.At(surfaceTemperature);
            var doublesCoverage = byDoubles[propellantIndex, pressureIndex]
                .PropellantParams.SkeletonCoverage.At(surfaceTemperature);

            Assert.Equal(unitsCoverage, doublesCoverage, 12);
        }

        // ... and the configured closure is genuinely in force, not the polynomial by accident.
        var propellant = propellants[0];
        var firstPressurePascals = Pressures(propellant).First();
        Assert.NotEqual(
            propellant.GetPocketSurfaceFraction(firstPressurePascals),
            byDoubles[0, 0].PropellantParams.SkeletonCoverage.At(750.0),
            6);
    }

    private static SkeletonSurfaceFractionSettings EquilibriumSettings() => new()
    {
        Mode = SkeletonSurfaceFractionMode.EquilibriumCarbon,
        EquilibriumTable = SkeletonCarbonEquilibriumTable.Load(CoverageTableJson)
    };

    private static IEnumerable<double> Pressures(Propellant propellant) =>
        propellant.PressureFrames?.Select(frame => frame.Pressure) ?? [1e6, 3.5e6, 6.5e6];

    private static Propellant[] LoadPropellants()
    {
        var json = File.ReadAllText(PropellantJson);

        return JsonSerializer.Deserialize<List<Propellant>>(
                   json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.ToArray()
               ?? throw new InvalidOperationException($"{PropellantJson} could not be read.");
    }

    /// <summary>Builds a one-frame table in a temp file so the interpolation can be pinned in isolation.</summary>
    private sealed class SkeletonCarbonEquilibriumTableFixture
    {
        public SkeletonCarbonEquilibriumTableFixture(double[] temperatures, double[] coverages)
        {
            var path = Path.Combine(Path.GetTempPath(), $"coverage-{Guid.NewGuid():N}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                surfaceTemperaturesKelvins = temperatures,
                propellants = new[]
                {
                    new { name = "X", frames = new[] { new { pressure = 1e6, coverages } } }
                }
            }));

            try
            {
                Curve = SkeletonCarbonEquilibriumTable.Load(path).CurveFor("X", 1e6);
            }
            finally
            {
                File.Delete(path);
            }
        }

        public SkeletonCoverageCurve Curve { get; }
    }
}
