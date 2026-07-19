using System.Globalization;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using UnitsNet.Units;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

/// <summary>
/// Prints, for every fuel at every pressure, the signed relative burn-rate error e = (calc/exp − 1)·100%,
/// then a per-fuel RMS and a per-composition / overall objective. This is the human-facing breakdown of
/// the scalar objective printed by <see cref="FitnessFunctionEvaluatorReport"/>: the fitness function is
/// mean_fuel( sqrt( mean_pressure( ((calc−exp)/exp)² ) ) ), so the per-fuel RMS shown here IS that fuel's
/// contribution, a composition value is the mean of its per-fuel RMS, and the overall value is the mean of
/// the three compositions (= <see cref="GroupOptimizationResult.AggregatedFitness"/>).
///
/// Works on the grouped result rather than the merged matrix so the Bas_2+Bas_3+Bas_4 group keeps its
/// collective 1/3 weight in the aggregate — a naive mean over the 5 merged fuels would not reconcile with
/// the objective.
/// </summary>
public class BurnRateErrorReport : PerCompositionPerFuelPdfReport
{
    // Same composition ordering / naming as GroupCombustionSolverParamsReport (raw 32-vector layout order).
    private static readonly string[] CompositionNamesValue =
        ["Bas_2 (includes Bas_3, Bas_4)", "Bas_1", "Bas_0"];

    // The fitness evaluator returns double.MaxValue for a composition in which any fuel's burn rate was not
    // found. A legitimate objective is O(0.01-1), so anything above this threshold (or non-finite) is that
    // sentinel and is rendered as a message rather than a ~1e308 number.
    private const double NonConvergedObjective = 1e6;

    public BurnRateErrorReport(GroupOptimizationResult groupResult) : base(groupResult)
    {
    }

    protected override IReadOnlyList<string> CompositionNames => CompositionNamesValue;

    protected override string Title => "Burn-Rate Error Breakdown";

    protected override string Introduction =>
        "Per-point relative error e = (calc/exp - 1)*100%. A fuel's RMS is its contribution to the "
        + "objective (sqrt of the mean squared fractional error over all pressures); a composition "
        + "objective is the mean of its per-fuel RMS, and the overall objective is the mean of the three "
        + "compositions. Numbers below reflect this report's input vector.";

    protected override string CompositionSectionSuffix => "Burn-Rate Errors";

    protected override void AppendCompositionFooter(
        Queue<IPdfOperation> operations,
        Optimization.Models.OptimizationProblemByUnits context,
        int compositionIndex)
    {
        operations.Enqueue(new PrintTextOperation(
            "Composition objective (mean per-fuel RMS) = " + FormatObjective(context.FitnessFunctionValue),
            TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
    }

    protected override void AppendReportFooter(
        Queue<IPdfOperation> operations)
    {
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
            "Overall objective (mean of the three compositions) = "
            + FormatObjective(GroupResult.AggregatedFitness),
            TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
    }

    private static string FormatObjective(double value)
    {
        if (double.IsFinite(value) == false || value >= NonConvergedObjective)
            return "non-converged (a fuel's burn rate was not found)";

        return string.Format(CultureInfo.InvariantCulture, "{0:0.0000} ({1:0.00}%)", value, value * 100.0);
    }

    protected override void AppendFuel(
        Queue<IPdfOperation> operations,
        Optimization.Models.OptimizationProblemByUnits context,
        int fuel)
    {
        operations.Enqueue(new PrintTextOperation(
            context.ProblemContextMatrix[fuel, 0].Propellant.Name, TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        var sumSquaredFraction = 0.0;
        for (var pressure = 0; pressure < context.PressureCount; pressure++)
        {
            var problemContext = context.ProblemContextMatrix[fuel, pressure];
            var calc = problemContext.MixedCombustionParams.BurnRate.As(SpeedUnit.MillimeterPerSecond);
            var exp = context.ExperimentalBurnRates[fuel, pressure].As(SpeedUnit.MillimeterPerSecond);
            var pressureMpa = problemContext.Pressure.As(PressureUnit.Megapascal);

            var fraction = (calc - exp) / exp;
            sumSquaredFraction += fraction * fraction;

            operations.Enqueue(new AddTabOperation());
            operations.Enqueue(new PrintTextOperation(
                string.Format(CultureInfo.InvariantCulture,
                    "p = {0:0.###} MPa: exp {1:0.0000}, calc {2:0.0000} mm/s -> err {3:+0.00;-0.00;0.00}%",
                    pressureMpa, exp, calc, fraction * 100.0),
                TextStyle.None));
            operations.Enqueue(new LineBreakOperation());
        }

        // RMS of the fractional errors over the fuel's pressures — the same quantity the objective sums.
        var rmsFraction = Math.Sqrt(sumSquaredFraction / context.PressureCount);
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CultureInfo.InvariantCulture,
                "Per-fuel RMS = {0:0.0000} ({1:0.00}%)", rmsFraction, rmsFraction * 100.0),
            TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
    }
}
