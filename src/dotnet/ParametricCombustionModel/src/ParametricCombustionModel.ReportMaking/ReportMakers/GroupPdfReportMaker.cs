using System.Collections.ObjectModel;
using System.Text;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Reports.Pdf;
using PdfSharp;
using PDFsharp.Api.Interfaces;

namespace ParametricCombustionModel.ReportMaking.ReportMakers;

public class GroupPdfReportMaker : IReportMaker, IPdfOperationVisitor
{
    private readonly IPdfGeneratorAdapter _pdfGeneratorAdapter;
    private readonly GroupReportContextDto _reportContext;

    public GroupPdfReportMaker(
        GroupReportContextDto reportContext,
        IPdfGeneratorAdapter pdfGeneratorAdapter)
    {
        _reportContext = reportContext ?? throw new ArgumentNullException(nameof(reportContext));
        _pdfGeneratorAdapter = pdfGeneratorAdapter ?? throw new ArgumentNullException(nameof(pdfGeneratorAdapter));
    }

    public string MakeReport()
    {
        var groupResult = _reportContext.GroupOptimizationResult;
        
        // Convert GroupOptimizationResult to OptimizationResult for compatibility with standard reports
        var mergedOptimizationResult = groupResult.ToOptimizationResult();
        
        // Create report components using the merged optimization result
        var penaltyEvaluatorsReport = new ConstraintPenaltyEvaluatorReport(mergedOptimizationResult);
        var parametricConstraintReport = new ParametricConstraintReport(mergedOptimizationResult);
        var differentialEvolutionSettingsReport = new DifferentialEvolutionSettingsReport(_reportContext.DifferentialEvolutionSettings);
        var groupCombustionSolverParamsReport = new GroupCombustionSolverParamsReport(groupResult);
        var fitnessFunctionEvaluatorReport = new FitnessFunctionEvaluatorReport(mergedOptimizationResult);
        var pressurePointCount = mergedOptimizationResult.OptimizedContext.ProblemContextMatrix.GetLength(1);
        var problemContextReport =
            new ProblemContextReport([0, pressurePointCount / 2, pressurePointCount - 1], mergedOptimizationResult);
        var pressureTablesReport = new PressureTablesReport([0, pressurePointCount / 2, pressurePointCount - 1], mergedOptimizationResult);
        var propellantReport = new PropellantReport(mergedOptimizationResult);
        var performanceReport = new GroupPerformanceMeterReport(_reportContext);

        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);
        
        // Add penalty evaluators report
        foreach (var operation in penaltyEvaluatorsReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add parametric constraint report
        foreach (var operation in parametricConstraintReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add differential evolution settings report
        foreach (var operation in differentialEvolutionSettingsReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add group combustion solver parameters report (replaces standard combustion solver params)
        foreach (var operation in groupCombustionSolverParamsReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add fitness function evaluator report
        foreach (var operation in fitnessFunctionEvaluatorReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add problem context report
        foreach (var operation in problemContextReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add pressure tables report
        foreach (var table in pressureTablesReport.Transform())
            _pdfGeneratorAdapter.AddTable(table);

        // Add burning rate plot
        _pdfGeneratorAdapter.AddImage("group_burning_rate_plot.jpg");
        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);

        // Add propellant report
        foreach (var operation in propellantReport.Transform())
            operation.Accept(this);
        
        // Add Python plots
        _pdfGeneratorAdapter.AddImage("lambda_gas_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("average_molar_mass_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("c_volume_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("temperatures_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("agglomeration_fraction_plot.png", isPortrait: false);
        _pdfGeneratorAdapter.AddImage("skeleton_surface_fraction_plot.png", isPortrait: false);

        // Add skeleton-layer plots (fitted solver outputs). Guarded so a missing
        // sidecar/Python failure degrades gracefully instead of breaking the PDF.
        foreach (var skeletonPlot in new[]
                 {
                     "skeleton_heat_flux_plot.png",
                     "skeleton_flame_heights_plot.png",
                     "skeleton_layer_geometry_plot.png",
                     "skeleton_temperatures_plot.png"
                 })
        {
            if (File.Exists(skeletonPlot))
                _pdfGeneratorAdapter.AddImage(skeletonPlot, isPortrait: false);
        }

        _pdfGeneratorAdapter.SetOrientation(PageOrientation.Portrait);
        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);

        // Add group performance report (replaces standard performance report)
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
        stringBuilder.AppendLine("Generated by ParametricCombustionModel (Group Optimization)");

        return stringBuilder.ToString();
    }
}
