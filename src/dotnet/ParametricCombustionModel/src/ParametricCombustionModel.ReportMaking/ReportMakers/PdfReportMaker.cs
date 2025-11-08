using System.Collections.ObjectModel;
using System.Text;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Reports.Pdf;
using PdfSharp;
using PDFsharp.Api.Interfaces;

namespace ParametricCombustionModel.ReportMaking.ReportMakers;

public class PdfReportMaker : IReportMaker, IPdfOperationVisitor
{
    private readonly IPdfGeneratorAdapter _pdfGeneratorAdapter;
    private readonly ReportContextDto _reportContext;

    public PdfReportMaker(
        ReportContextDto reportContext,
        IPdfGeneratorAdapter pdfGeneratorAdapter)
    {
        _reportContext = reportContext ?? throw new ArgumentNullException(nameof(reportContext));
        _pdfGeneratorAdapter = pdfGeneratorAdapter ?? throw new ArgumentNullException(nameof(pdfGeneratorAdapter));
    }

    public string MakeReport()
    {
        var optimizationResult = _reportContext.OptimizationResult;
        var penaltyEvaluatorsReport = new ConstraintPenaltyEvaluatorReport(optimizationResult);
        var parametricConstraintReport = new ParametricConstraintReport(optimizationResult);
        var differentialEvolutionSettingsReport = new DifferentialEvolutionSettingsReport(_reportContext);
        var combustionSolverParamsReport = new CombustionSolverParamsReport(optimizationResult);
        var fitnessFunctionEvaluatorReport = new FitnessFunctionEvaluatorReport(optimizationResult);
        var pressurePointCount = optimizationResult.OptimizedContext.ProblemContextMatrix.GetLength(1);
        var problemContextReport =
            new ProblemContextReport([0, pressurePointCount / 2, pressurePointCount - 1], optimizationResult);
        var pressureTablesReport = new PressureTablesReport([0, pressurePointCount / 2, pressurePointCount - 1], optimizationResult);
        var propellantReport = new PropellantReport(optimizationResult);
        var performanceReport = new PerformanceMeterReport(_reportContext);

        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);
        foreach (var operation in penaltyEvaluatorsReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        foreach (var operation in parametricConstraintReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        foreach (var operation in differentialEvolutionSettingsReport.Transform())
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

        _pdfGeneratorAdapter.SetOrientation(PageOrientation.Portrait);
        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);

        foreach (var operation in performanceReport.Transform())
            operation.Accept(this);

        _pdfGeneratorAdapter.AddFooterForLastPage(
            GetLastPageFooter());

        var generationResult = _pdfGeneratorAdapter.Generate();
        if (generationResult.IsSuccess == false)
            throw new InvalidOperationException("PDF generation failed: " + generationResult.Exception!.ToString());

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

        // var base64String = Convert.ToBase64String(_optimizationResult.BestSolverParams.ToArray()
        //                                                              .SelectMany(BitConverter.GetBytes).ToArray());
        // var indexOfSlice = base64String.Length / 2;
        // stringBuilder.AppendLine(base64String[..indexOfSlice]);
        // stringBuilder.AppendLine(base64String[indexOfSlice..]);

        stringBuilder.AppendLine("Generated by ParametricCombustionModel");

        return stringBuilder.ToString();
    }
}
