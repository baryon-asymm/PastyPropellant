using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Globalization;
using DotNetDifferentialEvolution.TerminationStrategies;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using ParametricCombustionModel.Telemetry;
using PastyPropellant.ConsoleApp;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.ConsoleApp.Scenarios;
using PastyPropellant.ConsoleApp.Scenarios.Settings;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PDFsharp.Api.Adapters;
using UnitsNet;

void GenerateReport(
    OptimizationResult optimizationResult, 
    string inputFileName, 
    DifferentialEvolutionScenarioSettings settings, 
    string cultureName, 
    string reportSuffix)
{
    // Set culture for localization
    var culture = new CultureInfo(cultureName);
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
    
    // Generate report filename with culture suffix
    string pdfOutputFileName = $"propellants.point_calculation.report.{reportSuffix}.pdf";
    
    var pdfGeneratorAdapter = new PdfSharpAdapter(pdfOutputFileName);
    var reportContextDto = new ReportContextDto(
        optimizationResult, 
        inputFileName, 
        settings.Propellants, 
        settings.DifferentialEvolutionSettings, 
        settings.Meter);
    
    var pdfReportMaker = new PdfReportMaker(reportContextDto, pdfGeneratorAdapter);
    pdfReportMaker.MakeReport();
    
    Console.WriteLine($"Report generated in {cultureName} culture: {pdfOutputFileName}");
}

async Task RunPointCalculationAsync()
{
    double[] lowerBound = [1, 1, 1, 5e4, 1, 5e4, 1, 5e4, 1.0, 1.0, 1.0, 1e-6, 1e-6, -1e12, 1e-6, 1.0, 2.0, 0.0];
    double[] upperBound = [1e12, 1e9, 1e12, 2e5, 1e12, 2e5, 1e12, 2e5, 10.0, 10.0, 10.0, 1.0, 1.0, 1e12, 1e1, 1.0, 2.0, 0.0];

    // Base parameter bounds
    /* double[] lowerBound = [
        1,      // ADecompose (from calculation)
        1,      // EDecompose (from calculation)
        3.26e+09,      // AKineticFlameInterPocket (from calculation)
        199999.99,     // EKineticFlameInterPocket (from calculation)
        2.62e+06,      // AKineticFlamePocketOutSkeleton (from calculation)
        50000,         // EKineticFlamePocketOutSkeleton (from calculation)
        6.21e+11,             // AKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        187465.54,           // EKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        2.2658901079487093,  // NuInterPocket (fixed from calculation)
        1.0000000000060636,   // NuPocketOutSkeleton (fixed from calculation)
        2.5395927015915345,  // NuPocketSkeleton (will be adjusted per scenario)
        9.73e-06,      // AMetalBurningConstant (from calculation)
        2.35e-08,      // BMetalBurningConstant (from calculation)
        -360438.99,    // DeltaH (from calculation)
        0.9961,        // KDiffusionHeight (from calculation)
        1.0,           // APowOrder (from calculation)
        2.0,           // BPowOrder (from calculation)
        0.0            // KCoefficientRadiationTemperature (from calculation)
    ];

    double[] upperBound = [
        double.MaxValue,      // ADecompose (fixed from calculation)
        1e9,      // EDecompose (fixed from calculation)
        3.26e+09,      // AKineticFlameInterPocket (from calculation)
        199999.99,     // EKineticFlameInterPocket (from calculation)
        2.62e+06,      // AKineticFlamePocketOutSkeleton (from calculation)
        50000,         // EKineticFlamePocketOutSkeleton (from calculation)
        6.21e+11,             // AKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        187465.54,           // EKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        2.2658901079487093,  // NuInterPocket (fixed from calculation)
        1.0000000000060636,   // NuPocketOutSkeleton (fixed from calculation)
        2.5395927015915345,  // NuPocketSkeleton (will be adjusted per scenario)
        9.73e-06,      // AMetalBurningConstant (from calculation)
        2.35e-08,      // BMetalBurningConstant (from calculation)
        -360438.99,    // DeltaH (from calculation)
        0.9961,        // KDiffusionHeight (from calculation)
        1.0,           // APowOrder (from calculation)
        2.0,           // BPowOrder (from calculation)
        0.0            // KCoefficientRadiationTemperature (from calculation)
    ]; */

    const string inputFileName = "propellants.234.json";

    const double penaltyRate = 1.0;
    const double heatFluxRatioThreshold = 100.0;
    const double poreDiameterThreshold = 3.0;

    var startTime = DateTime.Now;

    var meter = new PerformanceMeter();

    var maxInterPocketKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e9);
    var maxSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);
    var maxOutSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);

    IPenaltyEvaluator[] penaltyEvaluators = [
        new PocketHeatFluxRatioCompetitionPenaltyEvaluator(penaltyRate, heatFluxRatioThreshold),
        new InterPocketFasterBurnPenaltyEvaluator(penaltyRate),
        new KineticFlameHeatFluxPenaltyEvaluator(
            penaltyRate: penaltyRate,
            maxInterPocketKineticFlameHeatFlux: maxInterPocketKineticFlameHeatFlux,
            maxSkeletonKineticFlameHeatFlux: maxSkeletonKineticFlameHeatFlux,
            maxOutSkeletonKineticFlameHeatFlux: maxOutSkeletonKineticFlameHeatFlux),
        new PoreDiameterPenaltyEvaluator(penaltyRate, poreDiameterThreshold)
    ];

    var populationSize = lowerBound.Length * 64;
    var maxAvailableProcessors = Environment.ProcessorCount - 1;
    int processorsCount = maxAvailableProcessors;
    for (; processorsCount >= 14; processorsCount--)
        if (populationSize % processorsCount == 0)
            break;

    var settings = DifferentialEvolutionScenarioSettings
                    .CreateBuilder()
                    .WithMeter(meter)
                    .WithPropellantsFromFile(inputFileName)
                    .WithPopulationSize(populationSize)
                    .WithLowerBound(lowerBound)
                    .WithUpperBound(upperBound)
                    .WithMutationForce(0.7)
                    .WithCrossoverProbability(0.9)
                    .WithTerminationStrategy(
                        new TimeoutTerminationStrategy(TimeSpan.FromHours(20)))
                    .AddPenaltyEvaluators(penaltyEvaluators)
                    .WithProcessorsCount(processorsCount)
                    .Build();

    var scenario = new DifferentialEvolutionScenario(settings);

    Console.WriteLine("Starting point calculation with custom bounds:");
    Console.WriteLine($"- Input file: {inputFileName}");
    Console.WriteLine($"- Pore diameter threshold: {poreDiameterThreshold}");
    Console.WriteLine($"- Population size: {populationSize}");

    OptimizationResult result;

    using (meter.GetTotalExecutionTimeMeasurer().StartFrame())
    {
        var operationResult = await scenario.RunAsync();
        if (operationResult.IsSuccess == false)
        {
            Console.WriteLine("Point calculation failed:");
            Console.WriteLine(operationResult.Exception);
            return;
        }

        result = operationResult.Value!;
    }

    var plotSettings = new PlotSettings
    {
        Title = "Burning Rates - Point Calculation",
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
    if (result is not null)
    {
        plotRenderer.Render(result, plotSettings);
    }

    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper(
        "../../../../../src/python/PropellantsPlotRendering/src/main.py");

    var plotsResult = await propellantPlotsRenderingHelper.RenderPlotsAsync(inputFileName);

    if (result is not null)
    {
        // Generate Russian report
        GenerateReport(result, inputFileName, settings, "ru-RU", "ru");

        // Generate English report
        GenerateReport(result, inputFileName, settings, "en-US", "en");

        // Generate French report
        GenerateReport(result, inputFileName, settings, "fr-FR", "fr");
    }

    Console.WriteLine("Point calculation completed successfully");
    Console.WriteLine("Reports generated in Russian, English, and French");
}

// [Keep all your existing helper methods unchanged]
ReadOnlyCollection<Pressure> GetPressures()
{
    var maxPressure = Pressure.FromMegapascals(6.5);
    var minPressure = Pressure.FromMegapascals(1);
    const int pressurePoints = 10;
    return new ReadOnlyCollection<Pressure>(Enumerable.Range(0, pressurePoints)
        .Select(x => minPressure + (maxPressure - minPressure) / (pressurePoints - 1) * x)
        .ToArray());
}

EventBus<InfoLogEvent>.Subscribe(logEvent => {
    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] ({logEvent.Sender ?? "none"}) | {logEvent.Message}");
});

try
{
    // await RunAllScenariosAsync();

    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("Running Thermodynamics Scenario");
    Console.WriteLine(new string('=', 50) + "\n");

    // await RunThermodynamicsScenarioAsync();

    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine("Running Point Calculation");
    Console.WriteLine(new string('=', 50) + "\n");

    await RunPointCalculationAsync();
    
    // await RunAllScenariosAsy// );
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
