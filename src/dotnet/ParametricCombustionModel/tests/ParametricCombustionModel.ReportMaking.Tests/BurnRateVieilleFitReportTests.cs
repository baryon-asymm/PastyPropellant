using System.Globalization;
using System.Text.RegularExpressions;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Reports.Pdf;

namespace ParametricCombustionModel.ReportMaking.Tests;

/// <summary>
/// Numeric coverage for the log-log ordinary-least-squares fit in <see cref="BurnRateVieilleFitReport"/>.
///
/// The fit is verified against datasets small enough to solve by hand; each test states the arithmetic it
/// expects. Assertions run on the report's own rendered text rather than on the private fit method, because
/// the printed coefficients are what a reader acts on — a correct fit formatted into the wrong slot is still
/// a wrong report.
/// </summary>
public class BurnRateVieilleFitReportTests
{
    /// <summary>Matches the report's calc line: "calc: A = 1.0000E-011, v = 1.0000 (R2 = 1.0000); dv = +0.5000".</summary>
    private static readonly Regex CalcLine = new(
        @"A = (?<a>\S+), v = (?<v>\S+) \(R2 = (?<r2>[^)]+)\); dv = (?<dv>\S+)",
        RegexOptions.Compiled);

    private sealed record Fit(double A, double Nu, double RSquared, double DeltaNu);

    /// <summary>
    /// Builds a group whose composition 0 carries the fuel under test and whose other two compositions carry
    /// a fuel that cannot be fitted, so the report emits exactly one calc line to assert on.
    /// </summary>
    private static GroupOptimizationResult MakeGroupWithSingleFittableFuel(
        IReadOnlyList<double> pascals,
        IReadOnlyList<(double MetersPerSecond, bool Converged)> points,
        double experimentalNu)
    {
        Assert.Equal(pascals.Count, points.Count);

        var propellant = ReportFixture.MakePropellant("FuelUnderTest", a: 1e-6, nu: experimentalNu);
        var subject = ReportFixture.MakeProblem([propellant], pascals);

        for (var pressure = 0; pressure < points.Count; pressure++)
        {
            var (metersPerSecond, converged) = points[pressure];
            if (converged)
                ReportFixture.SetConverged(subject, 0, pressure, metersPerSecond);
            else
                ReportFixture.SetNonConvergedWithStaleRate(subject, 0, pressure, metersPerSecond);
        }

        return ReportFixture.MakeGroupResult(subject, MakeFuelless(), MakeFuelless());
    }

    /// <summary>
    /// A composition carrying no fuels. The group report walks all three compositions, so the other two have
    /// to contribute no per-fuel lines at all — a filler fuel would emit either a calc line or a
    /// "non-fittable" line and make every assertion below ambiguous about which fuel it matched.
    /// </summary>
    private static OptimizationProblemByUnits MakeFuelless() =>
        ReportFixture.MakeProblem([], [1e6]);

    private static Fit ParseSingleCalcLine(GroupOptimizationResult groupResult)
    {
        var lines = ReportFixture.TextLines(new BurnRateVieilleFitReport(groupResult).Transform());
        var line = ReportFixture.SingleLineContaining(lines, "calc: A =");

        var match = CalcLine.Match(line);
        Assert.True(match.Success, $"Could not parse the calc line: '{line}'.");

        return new Fit(
            double.Parse(match.Groups["a"].Value, NumberStyles.Float, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["v"].Value, NumberStyles.Float, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["r2"].Value, NumberStyles.Float, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["dv"].Value, NumberStyles.Float, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Burn rates that are exactly U = 1e-11 · p over p = 1e6, 1e7, 1e8 Pa.
    ///
    /// By hand: ln U is linear in ln p with slope 1, so v = 1 exactly, the intercept is ln(1e-11) giving
    /// A = 1e-11, and every residual is zero so R2 = 1. The propellant's experimental exponent is 0.5, so
    /// dv = 1 - 0.5 = +0.5.
    /// </summary>
    [Fact]
    public void Fit_ExactPowerLaw_RecoversTheGeneratingCoefficientsWithUnitRSquared()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [1e6, 1e7, 1e8],
            [(1e-5, true), (1e-4, true), (1e-3, true)],
            experimentalNu: 0.5);

        var fit = ParseSingleCalcLine(groupResult);

        Assert.Equal(1e-11, fit.A, 15);
        Assert.Equal(1.0, fit.Nu, 4);
        Assert.Equal(1.0, fit.RSquared, 4);
        Assert.Equal(0.5, fit.DeltaNu, 4);
    }

    /// <summary>
    /// A deliberately imperfect dataset, so R2 is exercised away from its degenerate value of 1.
    ///
    /// Pressures 1e6, 1e7, 1e8 Pa put x = ln p at -L, 0, +L about the mean, with L = ln 10. Burn rates
    /// 1e-3 · {1, e, e^3} m/s put y = ln U at 0, 1, 3 about a mean of 4/3, so dy = {-4/3, -1/3, +5/3}.
    ///
    ///   Sxx = 2L^2,  Sxy = (-L)(-4/3) + 0 + (L)(5/3) = 3L,  Syy = (16 + 1 + 25)/9 = 14/3
    ///   v   = Sxy/Sxx = 3L / 2L^2 = 1.5 / ln 10        = 0.651442
    ///   R2  = Sxy^2 / (Sxx · Syy) = 9L^2 / (28L^2 / 3) = 27/28 = 0.964286
    ///
    /// The intercept is meanY - v·meanX = (ln 1e-3 + 4/3) - (1.5/ln 10)(7 ln 10) = -5.574422 - 10.5,
    /// so A = exp(-16.074422) = 1.0446e-7.
    /// </summary>
    [Fact]
    public void Fit_ImperfectData_MatchesTheHandComputedSlopeAndRSquared()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [1e6, 1e7, 1e8],
            [(1e-3, true), (1e-3 * Math.E, true), (1e-3 * Math.Exp(3.0), true)],
            experimentalNu: 0.4);

        var fit = ParseSingleCalcLine(groupResult);

        Assert.Equal(1.5 / Math.Log(10.0), fit.Nu, 4);
        Assert.Equal(0.6514, fit.Nu, 4);
        Assert.Equal(27.0 / 28.0, fit.RSquared, 4);
        Assert.Equal(0.9643, fit.RSquared, 4);
        Assert.Equal(1.0446e-7, fit.A, 11);

        // dv is reported against the propellant's own exponent, not against anything the fit derived.
        Assert.Equal(0.6514 - 0.4, fit.DeltaNu, 4);
    }

    /// <summary>
    /// A flat model curve: every burn rate identical across the pressures. The slope and the coefficient come
    /// out right — v = 0 and A = the common rate.
    ///
    /// R2 does not. The fit intends "Syy == 0 (all burn rates equal) => a flat line fits perfectly, so R2 is
    /// 1 by convention", but the guard is the exact test <c>syy &gt; 0.0</c> and Syy is never exactly zero
    /// here: mean(y) of three identical doubles differs from y in the last bit, leaving Syy at ~2.4e-30
    /// instead of 0. The convention branch is skipped and R2 evaluates to Sxy^2/(Sxx·Syy) = 0.
    ///
    /// This assertion therefore pins the CURRENT behaviour (R2 = 0), not the documented one. It is a real
    /// defect — a perfectly power-law-flat curve is reported as maximally non-power-law — and is reported
    /// rather than fixed here, since the fit is product code this change does not own. If the guard is ever
    /// made tolerance-based, this test will fail and should then be flipped to expect 1.
    /// </summary>
    [Fact]
    public void Fit_FlatBurnRateCurve_ReportsZeroSlopeAndCorrectCoefficientButDegenerateRSquared()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [1e6, 1e7, 1e8],
            [(2e-3, true), (2e-3, true), (2e-3, true)],
            experimentalNu: 0.5);

        var fit = ParseSingleCalcLine(groupResult);

        Assert.Equal(0.0, fit.Nu, 4);
        AssertRelative(2e-3, fit.A);

        // Known defect: documented as 1.0, actually 0.0. See the remarks above.
        Assert.Equal(0.0, fit.RSquared, 4);
    }

    /// <summary>
    /// The convergence guard, at the point it was subtlest. A non-converged context can hold a large positive
    /// stale burn rate; filtering on the rate's sign alone would fit it as if it were a result and wreck the
    /// slope. The surviving two points lie exactly on U = 1e-11 · p, so a correct fit is indistinguishable
    /// from the exact-power-law case above — any contamination shows up immediately.
    /// </summary>
    [Fact]
    public void Fit_NonConvergedPointWithStalePositiveRate_IsExcludedAndDisclosed()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [1e6, 1e7, 1e8],
            [(1e-5, true), (1e-4, true), (1e30, false)],
            experimentalNu: 0.5);

        var fit = ParseSingleCalcLine(groupResult);

        Assert.Equal(1e-11, fit.A, 15);
        Assert.Equal(1.0, fit.Nu, 4);

        var lines = ReportFixture.TextLines(new BurnRateVieilleFitReport(groupResult).Transform());
        var disclosure = ReportFixture.SingleLineContaining(lines, "fitted on");
        Assert.Contains("fitted on 2 of 3 pressure points", disclosure, StringComparison.Ordinal);
        Assert.Contains("NOT CONVERGED", disclosure, StringComparison.Ordinal);
    }

    /// <summary>A fit over every point must not claim to be partial.</summary>
    [Fact]
    public void Fit_AllPointsConverged_DoesNotEmitThePartialFitDisclosure()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [1e6, 1e7, 1e8],
            [(1e-5, true), (1e-4, true), (1e-3, true)],
            experimentalNu: 0.5);

        var lines = ReportFixture.TextLines(new BurnRateVieilleFitReport(groupResult).Transform());

        Assert.DoesNotContain(lines, line => line.Contains("fitted on", StringComparison.Ordinal));
    }

    /// <summary>One usable point cannot determine a slope, so the report must decline rather than invent one.</summary>
    [Fact]
    public void Fit_FewerThanTwoConvergedPoints_IsReportedNonFittable()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [1e6, 1e7, 1e8],
            [(1e-5, true), (1e-4, false), (1e-3, false)],
            experimentalNu: 0.5);

        var lines = ReportFixture.TextLines(new BurnRateVieilleFitReport(groupResult).Transform());

        Assert.DoesNotContain(lines, line => line.Contains("calc: A =", StringComparison.Ordinal));
        ReportFixture.SingleLineContaining(lines, "non-fittable");
    }

    /// <summary>
    /// Converged points that all sit at one pressure give Sxx = 0 and an undefined slope. The report must
    /// decline instead of dividing by zero.
    /// </summary>
    [Fact]
    public void Fit_NoPressureSpread_IsReportedNonFittable()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [5e6, 5e6, 5e6],
            [(1e-3, true), (2e-3, true), (3e-3, true)],
            experimentalNu: 0.5);

        var lines = ReportFixture.TextLines(new BurnRateVieilleFitReport(groupResult).Transform());

        Assert.DoesNotContain(lines, line => line.Contains("calc: A =", StringComparison.Ordinal));
        ReportFixture.SingleLineContaining(lines, "non-fittable");
    }

    /// <summary>
    /// Non-positive rates have no logarithm. A zero or negative rate carrying a "found" flag must be dropped
    /// rather than turned into a NaN that propagates silently into A, v and R2.
    /// </summary>
    [Fact]
    public void Fit_NonPositiveConvergedRates_AreDroppedRatherThanProducingNaN()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [1e6, 1e7, 1e8, 1e9],
            [(0.0, true), (-1e-4, true), (1e-4, true), (1e-3, true)],
            experimentalNu: 0.5);

        var fit = ParseSingleCalcLine(groupResult);

        // Fitted on the two positive points only — (1e8, 1e-4) and (1e9, 1e-3) — which lie on U = 1e-12 · p.
        Assert.False(double.IsNaN(fit.Nu));
        Assert.False(double.IsNaN(fit.RSquared));
        Assert.Equal(1.0, fit.Nu, 4);
        AssertRelative(1e-12, fit.A);
    }

    /// <summary>
    /// Compares against the four-significant-figure value the report actually prints, so the tolerance is
    /// expressed relative to the magnitude rather than as a count of decimal places — the coefficients here
    /// span 1e-13 to 1e-7.
    /// </summary>
    private static void AssertRelative(double expected, double actual, double relativeTolerance = 1e-4)
    {
        var allowed = Math.Abs(expected) * relativeTolerance;
        Assert.True(Math.Abs(expected - actual) <= allowed,
                    $"Expected {expected:E4} within {relativeTolerance:P2}, but got {actual:E4}.");
    }

    /// <summary>
    /// The experimental coefficients are echoed from the propellant, not re-fitted, so they must be immune to
    /// whatever the model produced.
    /// </summary>
    [Fact]
    public void ExperimentalCoefficients_AreEchoedFromThePropellantNotRefitted()
    {
        var groupResult = MakeGroupWithSingleFittableFuel(
            [1e6, 1e7, 1e8],
            [(1e-5, true), (1e-4, true), (1e-3, true)],
            experimentalNu: 0.37);

        var lines = ReportFixture.TextLines(new BurnRateVieilleFitReport(groupResult).Transform());
        var expLine = lines.Single(line => line.StartsWith("exp:  A =", StringComparison.Ordinal));

        Assert.Contains("v = 0.3700", expLine, StringComparison.Ordinal);
    }
}
