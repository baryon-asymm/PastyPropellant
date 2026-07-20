using System.Globalization;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using ParametricCombustionModel.Telemetry;
using PDFsharp.Api.Adapters;

namespace PastyPropellant.ConsoleApp.Reporting;

/// <summary>
/// Renders the group optimisation PDF report for a <see cref="GroupOptimizationResult"/>.
///
/// <para>The optimisation run and the forward-eval replay both come through here, so a report of a
/// converged run and a report of the same vector replayed are produced by identical code.</para>
/// </summary>
public static class GroupReportGenerator
{
    /// <summary>
    /// Generates <c>propellants.group_optimization.report.&lt;reportSuffix&gt;.pdf</c> in the working
    /// directory and writes the confirmation line the console host has always printed.
    /// </summary>
    /// <param name="groupOptimizationResult">The fully populated result to report on.</param>
    /// <param name="inputFileName">Propellants file the run was fed, recorded in the report header.</param>
    /// <param name="cultureName">Culture the report is localised into (e.g. <c>en-US</c>).</param>
    /// <param name="reportSuffix">Suffix embedded in the output file name (e.g. <c>en</c>).</param>
    /// <param name="settings">DE settings to document; omitted for runs that had none.</param>
    /// <param name="meter">Performance meter whose timings the report includes.</param>
    public static void Generate(
        GroupOptimizationResult groupOptimizationResult,
        string inputFileName,
        string cultureName,
        string reportSuffix,
        DifferentialEvolutionSettings? settings = null,
        PerformanceMeter? meter = null)
    {
        // Set culture for localization
        var culture = new CultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // Generate report filename with culture suffix
        string pdfOutputFileName = $"propellants.group_optimization.report.{reportSuffix}.pdf";

        var pdfGeneratorAdapter = new PdfSharpAdapter(pdfOutputFileName);
        var reportContextDto = new GroupReportContextDto(
            groupOptimizationResult,
            inputFileName,
            settings,
            meter);

        var pdfReportMaker = new GroupPdfReportMaker(reportContextDto, pdfGeneratorAdapter);
        pdfReportMaker.MakeReport();

        Console.WriteLine($"Group report generated in {cultureName} culture: {pdfOutputFileName}");
    }
}
