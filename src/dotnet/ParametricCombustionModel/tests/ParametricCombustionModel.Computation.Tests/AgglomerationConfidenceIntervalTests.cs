using System.Text.Json;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Core.Models.PropellantComponents;

namespace ParametricCombustionModel.Computation.Tests;

/// <summary>
/// Pins the measured agglomerated-metal mass fraction Z_a^m that ships alongside the
/// <c>agglomeration_coefficients</c> polynomial.
///
/// <para><b>Why this needs pinning at all.</b> The intervals are read by nothing in the solver — they
/// exist so the agglomeration plots can show what the polynomial is an approximation OF. That makes the
/// binding silently breakable: rename the JSON key, or let the two checked-in copies of
/// <c>propellants.01234.json</c> drift apart, and the plots would quietly go back to drawing a bare
/// curve with no error bars and nothing would fail. These tests are the only thing that notices.</para>
///
/// <para>The values were digitised from Babuk's published Z_a^m(p) figure, so they carry the figure's
/// own reading error (~±0.002 absolute on Z, ~±0.03 MPa on the pressure). The test therefore pins the
/// STRUCTURE and the unit conventions rather than asserting the numbers to the last digit.</para>
/// </summary>
public class AgglomerationConfidenceIntervalTests
{
    private const string PropellantJson = @"propellants.01234.json";

    /// <summary>Every shipped composition carries its four measured points.</summary>
    [Fact]
    public void EveryPropellantCarriesFourMeasuredPoints()
    {
        foreach (var propellant in LoadPropellants())
        {
            var intervals = Aluminum(propellant).AgglomerationConfidenceIntervals;

            Assert.NotNull(intervals);
            Assert.Equal(4, intervals.Count());
        }
    }

    /// <summary>
    /// The pressures are in MEGApascals, matching the burn-rate <see cref="Propellant.ConfidenceIntervals"/>
    /// and NOT the Pa axis the agglomeration plots are drawn on. Getting this backwards is a factor of
    /// 10^6, so it is worth a test that would fail loudly rather than draw the bars off-screen.
    /// </summary>
    [Fact]
    public void PressuresAreInMegapascalsOverTheMeasuredWindow()
    {
        foreach (var propellant in LoadPropellants())
        {
            foreach (var interval in Aluminum(propellant).AgglomerationConfidenceIntervals!)
            {
                Assert.InRange(interval.XValue, 0.9, 6.6);
            }
        }
    }

    /// <summary>
    /// Z_a^m is a mass fraction in (0, 1) and the bar is a FULL height, so both whisker ends must stay
    /// on the physical side of zero. A half-height stored where a full height belongs would still pass
    /// the range check, which is why the bar is exercised through its ends rather than its size.
    /// </summary>
    [Fact]
    public void EveryWhiskerStaysInsideThePhysicalRange()
    {
        foreach (var propellant in LoadPropellants())
        {
            foreach (var interval in Aluminum(propellant).AgglomerationConfidenceIntervals!)
            {
                Assert.InRange(interval.YValue, 0.0, 1.0);
                Assert.True(interval.SizeOfConfidenceInterval > 0);
                Assert.InRange(interval.YValue - interval.SizeOfConfidenceInterval / 2, 0.0, 1.0);
                Assert.InRange(interval.YValue + interval.SizeOfConfidenceInterval / 2, 0.0, 1.0);
            }
        }
    }

    /// <summary>
    /// Every measured point agrees with the polynomial that approximates it.
    ///
    /// <para>This is the check that caught <c>Bas_0</c>: its shipped coefficients used to reproduce
    /// <c>Bas_3</c>'s curve rather than its own, and the tell was that the deviation was one-sided and
    /// grew with pressure (−0.1 % at 1.1 MPa to −9.1 % at 6.1 MPa) instead of scattering about zero.
    /// The coefficients were refitted to the dashed <c>Bas_0</c> curve of the source figure on
    /// 2026-08-11, which is why its tolerance is now the same order as everyone else's. Keep the
    /// tolerances tight enough that a repeat of that mix-up cannot hide inside them.</para>
    ///
    /// <para><c>Bas_2</c> is the one genuinely loose entry: its own points scatter about its own fit by
    /// 6.6 % RMS in BOTH directions, which the source figure itself shows and which is scatter, not a
    /// misassigned curve.</para>
    /// </summary>
    [Theory]
    [InlineData("Bas_1", 0.02)]     // worst point 1.5 %
    [InlineData("Bas_2", 0.12)]     // worst point 9.9 % — real scatter, both signs
    [InlineData("Bas_3", 0.02)]     // worst point 1.9 %
    [InlineData("Bas_4", 0.02)]     // worst point 1.8 %
    [InlineData("Bas_0", 0.04)]     // worst point 3.2 %, both signs, after the 2026-08-11 refit
    public void MeasuredPointsAgreeWithTheApproximatingPolynomial(string name, double tolerance)
    {
        var propellant = LoadPropellants().Single(candidate => candidate.Name == name);
        var coefficients = Aluminum(propellant).AgglomerationCoefficients.ToArray();

        foreach (var interval in Aluminum(propellant).AgglomerationConfidenceIntervals!)
        {
            var fitted = coefficients
                .Select((coefficient, power) => coefficient * Math.Pow(interval.XValue, power))
                .Sum();

            Assert.InRange(Math.Abs(fitted - interval.YValue) / interval.YValue, 0, tolerance);
        }
    }

    /// <summary>
    /// No composition's polynomial may miss its own measurements ONE-SIDEDLY AND WIDELY — the joint
    /// signature of a polynomial fitted to somebody else's curve.
    ///
    /// <para>This is the shape <c>Bas_0</c> had before 2026-08-11: all four deviations negative, growing
    /// monotonically to −9.1 %, because the coefficients traced <c>Bas_3</c>'s dotted curve. Either half
    /// of the signature alone is innocent — four same-sign deviations happen by chance one time in
    /// eight, and a 9 % miss is ordinary scatter for <c>Bas_2</c> — so the test fires only when both
    /// hold at once. That keeps it from blocking a legitimate refit while still catching the mix-up
    /// this file exists to prevent.</para>
    /// </summary>
    [Fact]
    public void NoPolynomialMissesItsOwnMeasurementsOneSidedlyAndWidely()
    {
        foreach (var propellant in LoadPropellants())
        {
            var coefficients = Aluminum(propellant).AgglomerationCoefficients.ToArray();

            var deviations = Aluminum(propellant).AgglomerationConfidenceIntervals!
                .Select(interval => (coefficients
                    .Select((coefficient, power) => coefficient * Math.Pow(interval.XValue, power))
                    .Sum() - interval.YValue) / interval.YValue)
                .ToArray();

            var oneSided = deviations.All(deviation => deviation > 0) || deviations.All(deviation => deviation < 0);
            var widest = deviations.Max(Math.Abs);

            Assert.False(
                oneSided && widest > 0.05,
                $"{propellant.Name}: the polynomial misses all four of its own measured points on the same "
                + $"side, by up to {widest:P1}. That is what a polynomial fitted to another composition's "
                + "curve looks like — check which curve of the source figure these coefficients trace.");
        }
    }

    private static Aluminum Aluminum(Propellant propellant) =>
        propellant.Components.OfType<Aluminum>().Single();

    private static Propellant[] LoadPropellants()
    {
        var json = File.ReadAllText(PropellantJson);

        return JsonSerializer.Deserialize<List<Propellant>>(
                   json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.ToArray()
               ?? throw new InvalidOperationException($"{PropellantJson} could not be read.");
    }
}
