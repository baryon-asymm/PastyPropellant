using System.Globalization;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using UnitsNet.Units;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

/// <summary>
/// For every fuel, reports the Vieille power-law coefficients U = A·p^v in the native (m/s, Pa) convention of
/// <see cref="ParametricCombustionModel.Core.Models.Propellant.A"/> / <c>.Nu</c> and of
/// <c>GetExperimentalBurnRate</c>.
///
/// The experimental side is the supplied A_exp/v_exp: the experimental burn rate is defined as exactly A·p^v,
/// so these are exact and are printed as-is (there is nothing to re-fit). The calculated side is an
/// ordinary-least-squares fit of the model burn rates in log-log space (ln U = ln A + v·ln p) over the fuel's
/// pressures; R² shows how power-law-like the model curve is, and dv = v_calc − v_exp is the model's error in
/// the pressure exponent — the headline goodness-of-fit-in-slope that the burn-rate error breakdown
/// (<see cref="BurnRateErrorReport"/>) does not summarise.
///
/// Works on the grouped result directly so it uses the same composition ordering as the rest of the group
/// report.
/// </summary>
public class BurnRateVieilleFitReport : PerCompositionPerFuelPdfReport
{
    // Same composition ordering / naming as GroupCombustionSolverParamsReport / BurnRateErrorReport.
    private static readonly string[] CompositionNamesValue =
        ["Bas_2 (includes Bas_3, Bas_4)", "Bas_1", "Bas_0"];

    public BurnRateVieilleFitReport(GroupOptimizationResult groupResult) : base(groupResult)
    {
    }

    protected override IReadOnlyList<string> CompositionNames => CompositionNamesValue;

    protected override string Title => "Vieille Power-Law Fit (U = A*p^v, U in m/s, p in Pa)";

    protected override string Introduction =>
        "Experimental A/v are the supplied Vieille coefficients (the experimental burn rate is defined as "
        + "exactly A*p^v, so they are exact, not re-fitted). Calculated A/v are an ordinary-least-squares "
        + "fit of the model burn rates in log-log space; R2 shows how power-law-like the model curve is, "
        + "and dv = v_calc - v_exp is the model's error in the pressure exponent. Numbers below reflect "
        + "this report's input vector.";

    protected override string CompositionSectionSuffix => "Vieille Coefficients";

    protected override void AppendFuel(
        Queue<IPdfOperation> operations,
        Optimization.Models.OptimizationProblemByUnits context,
        int fuel)
    {
        var propellant = context.ProblemContextMatrix[fuel, 0].Propellant;

        operations.Enqueue(new PrintTextOperation(propellant.Name, TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // Experimental side: exact, as supplied on the propellant.
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CultureInfo.InvariantCulture,
                "exp:  A = {0:E4}, v = {1:0.0000}", propellant.A, propellant.Nu),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        // Calculated side: log-log OLS of the model burn rates.
        operations.Enqueue(new AddTabOperation());
        var fit = FitCalculated(context, fuel);
        if (fit is null)
        {
            operations.Enqueue(new PrintTextOperation(
                "calc: non-fittable (fewer than two positive model burn rates)", TextStyle.None));
            operations.Enqueue(new LineBreakOperation());
            return;
        }

        var (aCalc, nuCalc, rSquared) = fit.Value;
        operations.Enqueue(new PrintTextOperation(
            string.Format(CultureInfo.InvariantCulture,
                "calc: A = {0:E4}, v = {1:0.0000} (R2 = {2:0.0000}); dv = {3:+0.0000;-0.0000;0.0000}",
                aCalc, nuCalc, rSquared, nuCalc - propellant.Nu),
            TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
    }

    /// <summary>
    /// Ordinary least squares of ln(U_calc) = ln(A) + v·ln(p) over the fuel's pressures, in the native
    /// (m/s, Pa) convention so A is directly comparable to <c>Propellant.A</c>. Points with a non-positive or
    /// non-finite model burn rate are dropped (e.g. a non-converged fuel whose burn rate is 0); returns null
    /// if fewer than two usable points remain or the pressures do not vary.
    /// </summary>
    private static (double A, double Nu, double RSquared)? FitCalculated(
        Optimization.Models.OptimizationProblemByUnits context, int fuel)
    {
        var xs = new List<double>(context.PressureCount);
        var ys = new List<double>(context.PressureCount);

        for (var pressure = 0; pressure < context.PressureCount; pressure++)
        {
            var problemContext = context.ProblemContextMatrix[fuel, pressure];
            var calc = problemContext.MixedCombustionParams.BurnRate.As(SpeedUnit.MeterPerSecond);
            var pascals = problemContext.Pressure.As(PressureUnit.Pascal);

            if (calc > 0.0 && double.IsFinite(calc) && pascals > 0.0)
            {
                xs.Add(Math.Log(pascals));
                ys.Add(Math.Log(calc));
            }
        }

        if (xs.Count < 2)
            return null;

        var n = xs.Count;
        var meanX = xs.Average();
        var meanY = ys.Average();

        var sxx = 0.0;
        var sxy = 0.0;
        var syy = 0.0;
        for (var i = 0; i < n; i++)
        {
            var dx = xs[i] - meanX;
            var dy = ys[i] - meanY;
            sxx += dx * dx;
            sxy += dx * dy;
            syy += dy * dy;
        }

        // No pressure spread => slope is undefined (all points at one pressure).
        if (sxx <= 0.0)
            return null;

        var nu = sxy / sxx;
        var intercept = meanY - nu * meanX;
        var a = Math.Exp(intercept);

        // R² = (Sxy)² / (Sxx·Syy) for simple linear regression; Syy == 0 (all burn rates equal) => a flat
        // line fits perfectly, so R² is 1 by convention.
        var rSquared = syy > 0.0 ? (sxy * sxy) / (sxx * syy) : 1.0;

        return (a, nu, rSquared);
    }
}
