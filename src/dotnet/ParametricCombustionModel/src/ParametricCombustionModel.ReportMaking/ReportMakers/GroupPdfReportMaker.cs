using System.Collections.ObjectModel;
using System.Globalization;
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
        // Report every pressure point, not just the 3 anchors (low / mid / high). Fitness uses all 10
        // points, so printing only 3 hid 7/10 of the fit; each pressure is its own table/text block
        // (width is set by propellant count, not pressure count), so this only lengthens the PDF.
        var allPressureIndexes = Enumerable.Range(0, pressurePointCount).ToArray();
        var problemContextReport =
            new ProblemContextReport(allPressureIndexes, mergedOptimizationResult);
        var pressureTablesReport = new PressureTablesReport(allPressureIndexes, mergedOptimizationResult);
        var propellantReport = new PropellantReport(mergedOptimizationResult);
        var performanceReport = new GroupPerformanceMeterReport(_reportContext);

        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);

        // Add self-identifying header (objective/penalty/algorithm/population/input/date) so an
        // archived PDF describes itself independently of its filename.
        var reportHeaderReport = new ReportHeaderReport(_reportContext);
        foreach (var operation in reportHeaderReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

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

        // Add reproduction vector (full-precision, copy-paste replayable via --forward-eval).
        // Sits right after the display-rounded parameter table it supersedes for replay.
        var reproductionVectorReport = new ReproductionVectorReport(_reportContext);
        _pdfGeneratorAdapter.AddParagraph(TextAlignment.Left);
        foreach (var operation in reproductionVectorReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add fitness function evaluator report
        foreach (var operation in fitnessFunctionEvaluatorReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add per-point / per-fuel burn-rate error breakdown of the objective just printed. Uses the
        // grouped result directly so composition/overall values reconcile exactly with the objective.
        var burnRateErrorReport = new BurnRateErrorReport(groupResult);
        foreach (var operation in burnRateErrorReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add Vieille power-law fit (A/v exp vs calc) per fuel: exp coefficients are supplied exactly,
        // calc coefficients are a log-log OLS fit of the model burn rates, so dv summarises the model's
        // pressure-exponent error that the per-point breakdown above does not.
        var burnRateVieilleFitReport = new BurnRateVieilleFitReport(groupResult);
        foreach (var operation in burnRateVieilleFitReport.Transform())
            operation.Accept(this);
        _pdfGeneratorAdapter.AddLineBreak();

        // Add flame-structure / heat-flux decomposition per point: which competing flame (out-skeleton
        // kinetic / skeleton / diffusion) dominates the surface heat feedback, plus flame heights and
        // surface/metal temperatures. Reads the same solver outputs SkeletonLayerPlotsHelper plots.
        var flameStructureReport = new FlameStructureReport(groupResult);
        foreach (var operation in flameStructureReport.Transform())
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
        var result = _reportContext.GroupOptimizationResult;

        stringBuilder.AppendLine("Generated at " + DateTime.Now);
        stringBuilder.AppendLine("Generated by ParametricCombustionModel (Group Optimization)");
        stringBuilder.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "Objective {0:G6} | Penalty {1:G6} | Input {2}",
            result.AggregatedFitness,
            result.TotalAggregatedPenalty,
            _reportContext.PropellantsFilePath));

        return stringBuilder.ToString();
    }
}
