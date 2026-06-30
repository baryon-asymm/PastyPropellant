using System.Globalization;
using ParametricCombustionModel.Optimization.Utils;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.PdfOperations;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

/// <summary>
/// Emits the full-precision result vector as a copy-paste-replayable block: a '#'-comment header
/// (objective, penalty, input file, layout, count, checksum) followed by one gene per line at
/// round-trip precision. Because <c>ReadVectorFile</c> ignores '#' lines and verifies the count and
/// checksum, the block copied out of the PDF is a valid <c>--forward-eval</c> input that reproduces
/// the run exactly — unlike the display-rounded parameter table elsewhere in the report.
/// </summary>
public class ReproductionVectorReport : ITransformable<Queue<IPdfOperation>>
{
    private readonly GroupReportContextDto _context;

    public ReproductionVectorReport(GroupReportContextDto context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();
        var result = _context.GroupOptimizationResult;
        var genes = result.BestParams;

        void Line(string text, TextStyle style = TextStyle.None)
        {
            operations.Enqueue(new PrintTextOperation(text, style));
            operations.Enqueue(new LineBreakOperation());
        }

        Line("Reproduction Vector", TextStyle.Bold | TextStyle.Underline);
        Line("Full-precision result vector. Copy the block below (including the '#' lines) into a text",
            TextStyle.Italic);
        Line("file and replay this run exactly with:  --forward-eval <file>", TextStyle.Italic);
        Line(string.Empty);

        // '#'-comment header — ignored by ReadVectorFile's parser (and its count/checksum are verified),
        // so the copied block is itself a valid, self-documenting forward-eval input.
        Line($"# reproduction vector ({genes.Length} params)");
        Line($"# objective (aggregated fitness) = {Invariant(result.AggregatedFitness)}");
        Line($"# penalty (total aggregated)     = {Invariant(result.TotalAggregatedPenalty)}");
        Line($"# input file = {_context.PropellantsFilePath}");
        if (_context.DifferentialEvolutionSettings is { } settings)
            Line($"# population = {settings.PopulationSize}");
        Line($"# generated  = {DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}");
        Line("# layout     = shared[0-10], Bas_2[11-17], Bas_1[18-24], Bas_0[25-31]");
        Line($"# count = {genes.Length}");
        Line($"# checksum = {VectorFormat.Checksum(genes)}");

        foreach (var geneLine in VectorFormat.FormatLines(genes))
            Line(geneLine);

        return operations;
    }

    private static string Invariant(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
