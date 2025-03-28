using System.Text;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Reports.Pdf;
using PDFsharp.Api.Interfaces;

namespace ParametricCombustionModel.ReportMaking.ReportMakers;

public class PdfReportMaker : IReportMaker, IPdfOperationVisitor
{
    private readonly IPdfGeneratorAdapter _pdfGeneratorAdapter;
    private readonly OptimizationResult _optimizationResult;

    private PdfReportMaker(
        OptimizationResult optimizationResult,
        IPdfGeneratorAdapter pdfGeneratorAdapter)
    {
        _optimizationResult = optimizationResult ?? throw new ArgumentNullException(nameof(optimizationResult));
        _pdfGeneratorAdapter = pdfGeneratorAdapter ?? throw new ArgumentNullException(nameof(pdfGeneratorAdapter));
    }

    public string MakeReport()
    {
        var penaltyEvaluatorsReport = new ConstraintPenaltyEvaluatorReport(_optimizationResult);
        var parametricConstraintReport = new ParametricConstraintReport(_optimizationResult);
        var combustionSolverParamsReport = new CombustionSolverParamsReport(_optimizationResult);
        var fitnessFunctionEvaluatorReport = new FitnessFunctionEvaluatorReport(_optimizationResult);
        var pressurePointCount = _optimizationResult.OptimizedContext.ProblemContextMatrix.GetLength(1);
        var problemContextReport =
            new ProblemContextReport([0, pressurePointCount / 2, pressurePointCount - 1], _optimizationResult);
        var pressureTablesReport = new PressureTablesReport([0, pressurePointCount / 2, pressurePointCount - 1], _optimizationResult);
        var propellantReport = new PropellantReport(_optimizationResult);

        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);
        foreach (var operation in penaltyEvaluatorsReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        foreach (var operation in parametricConstraintReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        foreach (var operation in combustionSolverParamsReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        foreach (var operation in fitnessFunctionEvaluatorReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        foreach (var operation in problemContextReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        foreach (var table in pressureTablesReport.Transform())
            _pdfGeneratorAdapter.AddTable(table);

        _pdfGeneratorAdapter.AddImage("burning_rate_plot.jpg");
        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);

        foreach (var operation in propellantReport.Transform())
            operation.Accept(this);
        // _pdfGeneratorAdapter.AddLineBreak();
        
        _pdfGeneratorAdapter.AddImage("lambda_gas_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("average_molar_mass_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("c_volume_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("temperatures_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("agglomeration_fraction_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("skeleton_surface_fraction_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);

        _pdfGeneratorAdapter.AddFooterForLastPage(
            GetLastPageFooter());

        _pdfGeneratorAdapter.Generate();

        return string.Empty;
    }

    public void Visit(
        AddTabOperation operation)
    {
        _pdfGeneratorAdapter.AddTab();
    }

    public void Visit(
        LineBreakOperation operation)
    {
        _pdfGeneratorAdapter.AddLineBreak();
    }

    public void Visit(
        PrintTextOperation operation)
    {
        var useBold = operation.Style.HasFlag(TextStyle.Bold);
        var useItalic = operation.Style.HasFlag(TextStyle.Italic);
        var useUnderline = operation.Style.HasFlag(TextStyle.Underline);

        _pdfGeneratorAdapter.AddText(operation.Text.Replace('\u207b', '\u00af'),
                                     useBold: useBold,
                                     useItalic: useItalic,
                                     useUnderline: useUnderline);
    }

    private string GetLastPageFooter()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("Generated at " + DateTime.Now);

        var base64String = Convert.ToBase64String(_optimizationResult.BestSolverParamsBySpan.ToArray()
                                                                     .SelectMany(BitConverter.GetBytes).ToArray());
        var indexOfSlice = base64String.Length / 2;
        stringBuilder.AppendLine(base64String[..indexOfSlice]);
        stringBuilder.AppendLine(base64String[indexOfSlice..]);

        stringBuilder.AppendLine("Generated by ParametricCombustionModel");

        return stringBuilder.ToString();
    }

    public static PdfReportMaker FromOptimizationResult(
        OptimizationResult optimizationResult,
        IPdfGeneratorAdapter pdfGeneratorAdapter)
    {
        return new PdfReportMaker(optimizationResult, pdfGeneratorAdapter);
    }
}
