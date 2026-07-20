using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Reports.Pdf;
using UnitsNet.Units;

namespace ParametricCombustionModel.ReportMaking.Tests;

/// <summary>
/// Numeric coverage for the RMS aggregation in <see cref="BurnRateErrorReport"/>, and for the rule that a
/// non-converged point must suppress the aggregate rather than quietly drop out of it.
///
/// A partial RMS is the dangerous failure here: it is computed only over the points that survived, so it
/// understates the failure and reads as a good fit. The commit that introduced the guard recorded a live
/// report publishing "Per-fuel RMS = 1.7919 (179.19%)" for a fuel that had failed at 9 of its 10 pressures.
/// </summary>
public class BurnRateErrorReportTests
{
    /// <summary>
    /// Pressures chosen so the experimental rates are whole numbers of mm/s: with A = 1e-6 and nu = 0.5,
    /// exp = 1e-6 · sqrt(p), giving 1, 2 and 3 mm/s at 1, 4 and 9 MPa.
    /// </summary>
    private static readonly double[] Pascals = [1e6, 4e6, 9e6];

    /// <summary>
    /// A group whose composition 0 holds one fuel at the three pressures above and whose other two
    /// compositions hold no fuels, so exactly one per-fuel block is emitted.
    /// </summary>
    private static GroupOptimizationResult MakeGroup(out OptimizationProblemByUnits subject)
    {
        subject = ReportFixture.MakeProblem([ReportFixture.MakePropellant("Bas_1", a: 1e-6, nu: 0.5)], Pascals);
        return ReportFixture.MakeGroupResult(subject,
                                             ReportFixture.MakeProblem([], [1e6]),
                                             ReportFixture.MakeProblem([], [1e6]));
    }

    /// <summary>Sets the calculated rate to the experimental rate scaled by (1 + fraction).</summary>
    private static void SetFractionalError(OptimizationProblemByUnits problem, int pressure, double fraction)
    {
        var experimental = problem.ExperimentalBurnRates[0, pressure].As(SpeedUnit.MeterPerSecond);
        ReportFixture.SetConverged(problem, 0, pressure, experimental * (1.0 + fraction));
    }

    /// <summary>
    /// The fixture must actually produce the experimental rates the arithmetic below assumes; if the
    /// experimental-rate derivation ever changes, this fails first and explains the others.
    /// </summary>
    [Fact]
    public void Fixture_ExperimentalRatesAreOneTwoAndThreeMillimetresPerSecond()
    {
        MakeGroup(out var subject);

        Assert.Equal(1.0, subject.ExperimentalBurnRates[0, 0].As(SpeedUnit.MillimeterPerSecond), 9);
        Assert.Equal(2.0, subject.ExperimentalBurnRates[0, 1].As(SpeedUnit.MillimeterPerSecond), 9);
        Assert.Equal(3.0, subject.ExperimentalBurnRates[0, 2].As(SpeedUnit.MillimeterPerSecond), 9);
    }

    /// <summary>
    /// By hand: fractional errors of +0.10, -0.20 and +0.20 give a sum of squares of
    /// 0.01 + 0.04 + 0.04 = 0.09, a mean of 0.03 over the three pressures, and an RMS of
    /// sqrt(0.03) = 0.173205 — printed as 0.1732 (17.32%).
    /// </summary>
    [Fact]
    public void PerFuelRms_IsTheRootMeanSquareOfTheFractionalErrors()
    {
        var groupResult = MakeGroup(out var subject);
        SetFractionalError(subject, 0, +0.10);
        SetFractionalError(subject, 1, -0.20);
        SetFractionalError(subject, 2, +0.20);

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());
        var rms = ReportFixture.SingleLineContaining(lines, "Per-fuel RMS");

        Assert.Equal("Per-fuel RMS = 0.1732 (17.32%)", rms);
    }

    /// <summary>
    /// The mean is taken over every pressure, so a fuel that is exact at two of three pressures must not be
    /// reported as exact. Errors of 0, 0 and +0.30 give sqrt(0.09/3) = 0.173205 — the same RMS as the case
    /// above, which is the point: the divisor is the pressure count, not the count of erroring points.
    /// </summary>
    [Fact]
    public void PerFuelRms_DividesByThePressureCountNotByTheCountOfErroringPoints()
    {
        var groupResult = MakeGroup(out var subject);
        SetFractionalError(subject, 0, 0.0);
        SetFractionalError(subject, 1, 0.0);
        SetFractionalError(subject, 2, +0.30);

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());
        var rms = ReportFixture.SingleLineContaining(lines, "Per-fuel RMS");

        Assert.Equal("Per-fuel RMS = 0.1732 (17.32%)", rms);
    }

    /// <summary>A perfect fit is the one case where the RMS must be exactly zero.</summary>
    [Fact]
    public void PerFuelRms_IsZeroWhenEveryCalculatedRateMatchesTheExperiment()
    {
        var groupResult = MakeGroup(out var subject);
        for (var pressure = 0; pressure < Pascals.Length; pressure++)
            SetFractionalError(subject, pressure, 0.0);

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());

        Assert.Equal("Per-fuel RMS = 0.0000 (0.00%)", ReportFixture.SingleLineContaining(lines, "Per-fuel RMS"));
    }

    /// <summary>Per-point errors are signed and reported against the experiment.</summary>
    [Fact]
    public void PerPointErrors_AreSignedFractionalErrorsAgainstTheExperiment()
    {
        var groupResult = MakeGroup(out var subject);
        SetFractionalError(subject, 0, +0.10);
        SetFractionalError(subject, 1, -0.20);
        SetFractionalError(subject, 2, +0.20);

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());

        Assert.Contains("p = 1 MPa: exp 1.0000, calc 1.1000 mm/s -> err +10.00%", lines);
        Assert.Contains("p = 4 MPa: exp 2.0000, calc 1.6000 mm/s -> err -20.00%", lines);
        Assert.Contains("p = 9 MPa: exp 3.0000, calc 3.6000 mm/s -> err +20.00%", lines);
    }

    /// <summary>
    /// The core of the guard. One non-converged point out of three must suppress the RMS outright and say how
    /// many points were missing. The two surviving points carry a 20% error each; if the RMS were computed
    /// over the survivors it would print 0.2000 (20.00%), so that value appearing anywhere is the regression.
    /// </summary>
    [Fact]
    public void PerFuelRms_IsSuppressedEntirelyWhenAnyPointDidNotConverge()
    {
        var groupResult = MakeGroup(out var subject);
        SetFractionalError(subject, 0, +0.20);
        SetFractionalError(subject, 1, +0.20);
        ReportFixture.SetNonConvergedWithStaleRate(subject, 0, 2, 0.0036);

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());
        var rms = ReportFixture.SingleLineContaining(lines, "Per-fuel RMS");

        Assert.Equal("Per-fuel RMS = NOT CONVERGED (1 of 3 pressure points had no burn rate)", rms);
        Assert.DoesNotContain(lines, line => line.Contains("0.2000 (20.00%)", StringComparison.Ordinal));
    }

    /// <summary>
    /// A non-converged point contributes no error line of its own either — the stale rate must not be
    /// differenced against the experiment.
    /// </summary>
    [Fact]
    public void PerPointLine_ForANonConvergedPointReportsNoErrorRatherThanAStaleOne()
    {
        var groupResult = MakeGroup(out var subject);
        SetFractionalError(subject, 0, +0.10);
        SetFractionalError(subject, 1, +0.10);
        ReportFixture.SetNonConvergedWithStaleRate(subject, 0, 2, 0.0036);

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());

        Assert.Contains("p = 9 MPa: exp 3.0000 mm/s, calc NOT CONVERGED -> no error", lines);

        // "-> err" is the converged form; the non-converged line ends "-> no error", which must not be
        // mistaken for it.
        Assert.DoesNotContain(lines, line => line.Contains("p = 9 MPa", StringComparison.Ordinal)
                                             && line.Contains("-> err", StringComparison.Ordinal));
    }

    /// <summary>Every point failing must not degrade into a zero RMS over an empty sum.</summary>
    [Fact]
    public void PerFuelRms_IsSuppressedWhenNoPointConvergedAtAll()
    {
        var groupResult = MakeGroup(out var subject);
        for (var pressure = 0; pressure < Pascals.Length; pressure++)
            ReportFixture.SetNonConvergedWithStaleRate(subject, 0, pressure, 0.003);

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());
        var rms = ReportFixture.SingleLineContaining(lines, "Per-fuel RMS");

        Assert.Equal("Per-fuel RMS = NOT CONVERGED (3 of 3 pressure points had no burn rate)", rms);
    }

    /// <summary>
    /// The composition and overall objectives are read off the result, and the sentinel the fitness evaluator
    /// uses for a failed composition must render as a failure rather than as an enormous but plausible number.
    /// </summary>
    [Fact]
    public void Objectives_RenderTheNonConvergedSentinelAsAFailure()
    {
        var groupResult = MakeGroup(out var subject);
        for (var pressure = 0; pressure < Pascals.Length; pressure++)
            SetFractionalError(subject, pressure, 0.0);

        subject.FitnessFunctionValue = double.MaxValue;
        groupResult.AggregatedFitness = double.MaxValue;

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());

        Assert.Contains("Composition objective (mean per-fuel RMS) = non-converged (a fuel's burn rate was not found)",
                        lines);
        Assert.Contains("Overall objective (mean of the three compositions) = non-converged (a fuel's burn rate was not found)",
                        lines);
    }

    /// <summary>A finite objective is printed as a number, so the sentinel check above is not vacuous.</summary>
    [Fact]
    public void Objectives_RenderAFiniteValueAsANumber()
    {
        var groupResult = MakeGroup(out var subject);
        for (var pressure = 0; pressure < Pascals.Length; pressure++)
            SetFractionalError(subject, pressure, 0.0);

        groupResult.AggregatedFitness = 0.1234;

        var lines = ReportFixture.TextLines(new BurnRateErrorReport(groupResult).Transform());

        Assert.Contains("Overall objective (mean of the three compositions) = 0.1234 (12.34%)", lines);
    }
}
