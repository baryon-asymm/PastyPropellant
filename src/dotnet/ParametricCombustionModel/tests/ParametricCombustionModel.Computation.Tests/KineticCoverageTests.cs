using System.Text.Json;
using ParametricCombustionModel.Computation.Extensions;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Core.Models;

namespace ParametricCombustionModel.Computation.Tests;

/// <summary>
/// Pins the kinetic-coverage closure against the measurement it was calibrated on, and against the two
/// claims that make it worth having.
///
/// <para><b>Why these properties.</b> The closure earns its place by three things a polynomial cannot do:
/// it stays inside (0, 1] whatever the recipe, so it can reach a coverage that exceeds the pocket's
/// condensed inventory; it depends on the recipe only through the fine-oxidiser fraction, which is what
/// makes it transferable across oxidiser dispersity instead of tabulated per composition; and it is
/// independent of the surface temperature, so it adds no gain to the bisection the solver relies on. Each
/// is checked below. The accuracy test is deliberately stated against Babuk's measured agglomeration
/// curve — the shipped polynomial is that measurement, so the polynomial is the reference here, not the
/// rival.</para>
/// </summary>
public class KineticCoverageTests
{
    private const string PropellantJson = @"propellants.01234.json";

    /// <summary>The compositions the constants were calibrated on: one binder, one additive, AP varied.</summary>
    private static readonly string[] CalibrationSet = ["Bas_2", "Bas_3", "Bas_4"];

    /// <summary>
    /// The closure reproduces the measured coverage of its calibration set.
    ///
    /// <para>Bound at 25 %, against a fitted in-sample RMS of 4.7..8.6 % and a worst point of 18.6 %: the
    /// test exists to catch a broken constant or a lost recipe input, not to re-assert the fit quality,
    /// which lives in <c>screen_kinetic_coverage_forms.py</c> where it can be re-derived.</para>
    /// </summary>
    [Fact]
    public void ReproducesTheMeasuredCoverageOfItsCalibrationSet()
    {
        var settings = new SkeletonSurfaceFractionSettings
        {
            Mode = SkeletonSurfaceFractionMode.KineticCoverage
        };

        foreach (var propellant in LoadPropellants().Where(p => CalibrationSet.Contains(p.Name)))
        foreach (var pressurePascals in Pressures(propellant))
        {
            var measured = propellant.GetPocketSurfaceFraction(pressurePascals);
            var computed = propellant.GetSkeletonCoverage(pressurePascals, settings).At(700.0);

            Assert.InRange(computed / measured, 0.75, 1.25);
        }
    }

    /// <summary>
    /// Coverage stays a surface fraction for every shipped composition and pressure — including the one
    /// whose measured coverage exceeds its own condensed inventory, which is the reason the closure is a
    /// rate balance rather than an inventory.
    /// </summary>
    [Fact]
    public void CoverageStaysAFractionEverywhere()
    {
        var settings = new SkeletonSurfaceFractionSettings
        {
            Mode = SkeletonSurfaceFractionMode.KineticCoverage
        };

        foreach (var propellant in LoadPropellants())
        foreach (var pressurePascals in Pressures(propellant))
        {
            var coverage = propellant.GetSkeletonCoverage(pressurePascals, settings).At(700.0);

            Assert.InRange(coverage, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Coverage falls with pressure wherever the pocket holds fine oxidiser, and is flat where it does
    /// not.
    ///
    /// <para>This is the closure's whole physical claim, and it is the one the measurement makes too:
    /// the all-coarse composition is measured flat (0.529 → 0.522 over 1..6.5 MPa) while the others fall
    /// by roughly a factor of two. A closure that decayed everywhere would reproduce four curves and
    /// contradict the fifth.</para>
    /// </summary>
    [Fact]
    public void PressureDecayFollowsTheFineOxidiserAndNothingElse()
    {
        var settings = new SkeletonSurfaceFractionSettings
        {
            Mode = SkeletonSurfaceFractionMode.KineticCoverage
        };

        foreach (var propellant in LoadPropellants())
        {
            var low = propellant.GetSkeletonCoverage(1e6, settings).At(700.0);
            var high = propellant.GetSkeletonCoverage(6.5e6, settings).At(700.0);

            if (propellant.GetFineOxidiserMassFraction() == 0.0)
                Assert.Equal(low, high, 12);
            else
                Assert.True(high < low,
                    $"{propellant.Name} holds fine oxidiser but its coverage did not fall with pressure.");
        }
    }

    /// <summary>
    /// Coverage does not depend on the surface temperature, so the closure adds no loop gain to the
    /// bisection.
    /// </summary>
    [Fact]
    public void CoverageIsFlatAcrossTheSurfaceTemperatureBracket()
    {
        var settings = new SkeletonSurfaceFractionSettings
        {
            Mode = SkeletonSurfaceFractionMode.KineticCoverage
        };

        foreach (var propellant in LoadPropellants())
        {
            var curve = propellant.GetSkeletonCoverage(3.5e6, settings);
            var reference = curve.At(SurfaceTemperatureSearchBounds.MinKelvins);

            foreach (var surfaceTemperature in new[]
                     {
                         SurfaceTemperatureSearchBounds.MinKelvins, 675.0, 750.0, 825.0,
                         SurfaceTemperatureSearchBounds.MaxKelvins
                     })
                Assert.Equal(reference, curve.At(surfaceTemperature));
        }
    }

    /// <summary>
    /// Two compositions with the same fine-oxidiser loading get the same coverage.
    ///
    /// <para>Stated as a test because it is the closure's honest limitation, not an accident: the recipe
    /// enters only through <c>w_fine</c>, so Bas_0, Bas_1 and Bas_2 — which differ by binder modifier and
    /// additive, not by dispersity — are indistinguishable to it. Anyone extending the closure with a
    /// per-binder channel will break this test, and that is the intended signal.</para>
    /// </summary>
    [Fact]
    public void CompositionsWithEqualFineOxidiserAreIndistinguishable()
    {
        var settings = new SkeletonSurfaceFractionSettings
        {
            Mode = SkeletonSurfaceFractionMode.KineticCoverage
        };

        var propellants = LoadPropellants();
        var reference = propellants.First(p => p.Name == "Bas_2");

        foreach (var propellant in propellants.Where(p => p.Name is "Bas_0" or "Bas_1"))
        {
            Assert.Equal(reference.GetFineOxidiserMassFraction(), propellant.GetFineOxidiserMassFraction(), 12);
            Assert.Equal(
                reference.GetSkeletonCoverage(1e6, settings).At(700.0),
                propellant.GetSkeletonCoverage(1e6, settings).At(700.0),
                12);
        }
    }

    /// <summary>
    /// Selecting the kinetic closure leaves the other two untouched, and the kinetic block is validated
    /// even when another mode is in force.
    /// </summary>
    [Fact]
    public void ModeSelectionAndValidationAreIndependent()
    {
        var propellant = LoadPropellants().First(p => p.Name == "Bas_3");

        var polynomial = propellant.GetSkeletonCoverage(1e6, SkeletonSurfaceFractionSettings.Default).At(700.0);
        var kinetic = propellant.GetSkeletonCoverage(
            1e6,
            new SkeletonSurfaceFractionSettings { Mode = SkeletonSurfaceFractionMode.KineticCoverage }).At(700.0);

        Assert.NotEqual(polynomial, kinetic);
        Assert.Equal(propellant.GetPocketSurfaceFraction(1e6), polynomial);

        // A broken kinetic block fails even under the historical closure, so it cannot lie dormant.
        var settings = new SkeletonSurfaceFractionSettings
        {
            Mode = SkeletonSurfaceFractionMode.Polynomial,
            Kinetic = new KineticCoverageSettings { FineOxidiserChannel = -1.0 }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new KineticCoverageSettings { BinderChannel = 0.0, FineOxidiserChannel = 0.0 }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new KineticCoverageSettings { ReferencePressurePascals = 0.0 }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new KineticCoverageSettings { PressureOrder = double.NaN }.Validate());
    }

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
}
