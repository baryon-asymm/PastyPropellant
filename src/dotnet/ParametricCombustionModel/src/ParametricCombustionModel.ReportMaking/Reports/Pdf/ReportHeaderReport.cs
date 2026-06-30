using System.Globalization;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.PdfOperations;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

/// <summary>
/// Self-identifying header printed at the very top of the group report: objective, penalty,
/// algorithm + population, parameter-vector dimension, input file and generation date. Makes an
/// archived PDF describe itself independently of its filename, so a reader can confirm at a glance
/// which run/settings produced it. The same metadata appears machine-readable in the Reproduction
/// Vector block (<see cref="ReproductionVectorReport"/>); this block is the human-facing summary.
/// </summary>
public class ReportHeaderReport : ITransformable<Queue<IPdfOperation>>
{
    // Slot layout of the grouped 32-parameter vector (see CLAUDE.md / GroupCombustionSolverParamsByDoubles).
    private const int GroupVectorLength = 32;
    private const string GroupLayoutLegend = "shared[0-10], Bas_2[11-17], Bas_1[18-24], Bas_0[25-31]";

    private readonly GroupReportContextDto _context;

    public ReportHeaderReport(GroupReportContextDto context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();
        var result = _context.GroupOptimizationResult;

        // Bold label + plain value on one line (the visitor concatenates PrintTextOperations until a break).
        void Field(string label, string value)
        {
            operations.Enqueue(new PrintTextOperation(label, TextStyle.Bold));
            operations.Enqueue(new PrintTextOperation(value, TextStyle.None));
            operations.Enqueue(new LineBreakOperation());
        }

        operations.Enqueue(new PrintTextOperation(
            "Group Optimization Report", TextStyle.Bold | TextStyle.Underline));
        operations.Enqueue(new LineBreakOperation());

        Field("Objective (aggregated fitness): ", Format(result.AggregatedFitness));
        Field("Penalty (total aggregated): ", Format(result.TotalAggregatedPenalty));

        if (_context.DifferentialEvolutionSettings is { } settings)
        {
            Field("Algorithm: ", DescribeAlgorithm(settings));
            Field("Population size: ", settings.PopulationSize.ToString(CultureInfo.InvariantCulture));
        }

        var count = result.BestParams.Length;
        var vectorText = count == GroupVectorLength
            ? $"{count} params ({GroupLayoutLegend})"
            : $"{count} params";
        Field("Parameter vector: ", vectorText);
        Field("Input file: ", _context.PropellantsFilePath);
        Field("Generated: ", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

        operations.Enqueue(new LineBreakOperation());

        return operations;
    }

    private static string DescribeAlgorithm(DifferentialEvolutionSettings settings)
    {
        var algorithm = settings.Strategy.ToString();

        if (settings.NelderMead.Enabled)
        {
            var stages = new List<string>();
            if (settings.NelderMead.MemeticInLoop) stages.Add("memetic");
            if (settings.NelderMead.FinalPolish) stages.Add("final polish");

            algorithm += stages.Count > 0
                ? $" + Nelder–Mead ({string.Join(", ", stages)})"
                : " + Nelder–Mead";
        }

        return algorithm;
    }

    private static string Format(double value) => value.ToString("G6", CultureInfo.InvariantCulture);
}
