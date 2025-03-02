
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;
using ParametricCombustionModel.ProcessWorker.Scenarios;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PDFsharp.Api.Adapters;

EventBus<InfoLogEvent>.Subscribe(logEvent => {
    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] ({logEvent.Sender ?? "none"}) | {logEvent.Message}");
});

double[] lowerBound = [1, 1, 1, 5e4, 1, 5e4, 1, 5e4, 0.1, 0.1, 0.1, 1, 1, -1e12, 1e-6, 0.0];
double[] upperBound = [double.MaxValue, 1e9, 1e12, 2e5, 1e12, 2e5, 1e12, 2e5, 10.0, 10.0, 10.0, 1e12, 1e9, 1e12, 3, 1.0];

var scenario = new DifferentialEvolutionRuntime(
    populationSize: lowerBound.Length * 8,
    maxStagnationStreak: 100_000,
    "propellants.json",
    lowerBound: lowerBound,
    upperBound: upperBound
);
var operationResult = await scenario.RunAsync();
if (operationResult.IsSuccess == false)
{
    Console.WriteLine(operationResult.Exception);
    return;
}

var settings = new PlotSettings
{
    Title = "Burning Rates",
    TitleFontSize = 22,
    XAxisMinimum = 0.9,
    XAxisMaximum = 6.6,
    XAxisTitle = "Pressure, MPa",
    YAxisTitle = "mm/s",
    Width = 800,
    Height = 800,
    Dpi = 96
};
var plotRenderer = new BurningRatePlotRenderer();
if (operationResult.Value is not null)
{
    plotRenderer.Render(operationResult.Value, settings);
}

var pdfGeneratorAdapter = new PdfSharpAdapter("report.pdf");
if (operationResult.Value is not null)
{
    var pdfReportMaker = PdfReportMaker.FromOptimizationResult(operationResult.Value, pdfGeneratorAdapter);
    pdfReportMaker.MakeReport();
}
