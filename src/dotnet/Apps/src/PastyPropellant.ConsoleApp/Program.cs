using System.Globalization;
using DotNetDifferentialEvolution.TerminationStrategies;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
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

void GenerateGroupReport(
    GroupOptimizationResult groupOptimizationResult,
    string inputFileName,
    string cultureName,
    string reportSuffix,
    System.Collections.ObjectModel.ReadOnlyCollection<Propellant> propellants,
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
        propellants,
        settings,
        meter);
    
    var pdfReportMaker = new GroupPdfReportMaker(reportContextDto, pdfGeneratorAdapter);
    pdfReportMaker.MakeReport();
    
    Console.WriteLine($"Group report generated in {cultureName} culture: {pdfOutputFileName}");
}

double[] GetLowerBound()
{
    return [0, 0, 0, 5e4, 0, 5e4, 0, 5e4, -3.0, -3.0, -3.0, 1e-15, 1e-15, -1e12, 1e-15, 1.0, 2.0, 0.0];
}

double[] GetUpperBound()
{
    return [double.MaxValue, 4422718, 1e15, 2e5, 1e15, 2e5, 1e15, 2e5, 3.0, 3.0, 3.0, 1.0, 1.0, 1e12, 1e1, 1.0, 2.0, 0.0];
}

// ========== GROUP OPTIMIZATION BOUNDS (32-ELEMENT VECTOR) ==========
// Structure: [0-10] shared, [11-17] Bas_2, [18-24] Bas_1, [25-31] Bas_0
double[] GetGroupLowerBound()
{
    var originalLower = GetLowerBound();
    var groupBounds = new double[32];
    
    // Map shared parameters [0-10] from original indices [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17]
    int[] commonIndices = [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17];
    for (int i = 0; i < commonIndices.Length; i++)
        groupBounds[i] = originalLower[commonIndices[i]];
    
    // Map specific parameters for each composition [11-31] from original indices [0, 1, 6, 7, 10, 11, 12]
    int[] specificIndices = [0, 1, 6, 7, 10, 11, 12];
    for (int composition = 0; composition < 3; composition++)
    {
        int blockStart = 11 + composition * 7;
        for (int i = 0; i < specificIndices.Length; i++)
            groupBounds[blockStart + i] = originalLower[specificIndices[i]];
    }
    
    return groupBounds;
}

double[] GetGroupUpperBound()
{
    var originalUpper = GetUpperBound();
    var groupBounds = new double[32];
    
    // Map shared parameters [0-10] from original indices [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17]
    int[] commonIndices = [2, 3, 4, 5, 8, 9, 13, 14, 15, 16, 17];
    for (int i = 0; i < commonIndices.Length; i++)
        groupBounds[i] = originalUpper[commonIndices[i]];
    
    // Map specific parameters for each composition [11-31] from original indices [0, 1, 6, 7, 10, 11, 12]
    int[] specificIndices = [0, 1, 6, 7, 10, 11, 12];
    for (int composition = 0; composition < 3; composition++)
    {
        int blockStart = 11 + composition * 7;
        for (int i = 0; i < specificIndices.Length; i++)
            groupBounds[blockStart + i] = originalUpper[specificIndices[i]];
    }
    
    return groupBounds;
}

async Task<(OperationResult<GroupOptimizationResult>? result, PerformanceMeter meter, DifferentialEvolutionSettings? deSettings, System.Collections.ObjectModel.ReadOnlyCollection<Propellant> propellants)> RunGroupOptimizationAsync(string inputFileName)
{
    const double penaltyRate = 0.01;
    const double heatFluxRatioThreshold = 100.0;
    const double poreDiameterThreshold = 3.0;
    const double largeOxidizerParticleSizeThreshold = 1.0;

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
        new PoreDiameterPenaltyEvaluator(penaltyRate, poreDiameterThreshold),
        new LargeOxidizerParticleSizePenaltyEvaluator(penaltyRate, largeOxidizerParticleSizeThreshold)
    ];

    var groupLowerBound = GetGroupLowerBound();
    var groupUpperBound = GetGroupUpperBound();

    var populationSize = groupLowerBound.Length * 12; // 32 * 12 = 384
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
                    .WithLowerBound(groupLowerBound)
                    .WithUpperBound(groupUpperBound)
                    .WithMutationForce(0.5)
                    .WithCrossoverProbability(0.9)
                    .WithTerminationStrategy(
                        new OrTerminationStrategy(
                            new CustomStagnationStreakTerminationStrategy(
                                maxStagnationStreak: 5_000,
                                relativeStagnationThreshold: 1e-6),
                            new TimeoutTerminationStrategy(TimeSpan.FromHours(9))
                        )
                    )
                    .AddPenaltyEvaluators(penaltyEvaluators)
                    .WithProcessorsCount(processorsCount)
                    .Build();

    var scenario = new GroupDifferentialEvolutionScenario(settings);

    Console.WriteLine("Starting group optimization (Bas_0, Bas_1, Bas_2+Bas_3+Bas_4):");
    Console.WriteLine($"- Input file: {inputFileName}");
    Console.WriteLine($"- Population size: {populationSize}");
    Console.WriteLine($"- Parameter vector size: 32 (11 shared + 7×3 specific)");
    Console.WriteLine($"- Processors: {processorsCount}\n");

    OperationResult<GroupOptimizationResult> operationResult;

    using (meter.GetTotalExecutionTimeMeasurer().StartFrame())
    {
        operationResult = await scenario.RunAsync();
    }

    return (operationResult, meter, settings.DifferentialEvolutionSettings, settings.Propellants);
}

EventBus<InfoLogEvent>.Subscribe(logEvent => {
    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] ({logEvent.Sender ?? "none"}) | {logEvent.Message}");
});

try
{
    Console.WriteLine(new string('=', 90));
    Console.WriteLine("GROUP OPTIMIZATION: BAS_0, BAS_1, BAS_2+BAS_3+BAS_4 (SIMULTANEOUS)");
    Console.WriteLine(new string('=', 90) + "\n");

    var (operationResult, meter, deSettings, propellants) = await RunGroupOptimizationAsync("propellants.01234.json");

    if (operationResult == null || !operationResult.IsSuccess)
    {
        Console.WriteLine("\n❌ Group optimization failed:");
        if (operationResult != null)
            Console.WriteLine(operationResult.Exception);
        return;
    }

    var groupResult = operationResult.Value;

    Console.WriteLine("\n✓ Group optimization completed successfully");
    Console.WriteLine($"  Aggregated fitness: {groupResult!.AggregatedFitness:E4}");
    Console.WriteLine($"  Total aggregated penalty: {groupResult.TotalAggregatedPenalty:E4}\n");

    // Display results for each composition (order matches the raw 32-vector layout)
    var compositionNames = new[] { "Bas_2+Bas_3+Bas_4", "Bas_1", "Bas_0" };
    Console.WriteLine("Individual Results:");
    for (int i = 0; i < 3; i++)
    {
        Console.WriteLine($"  {compositionNames[i]}:");
        Console.WriteLine($"    - Fitness: {groupResult.IndividualFitnesses[i]:E4}");
        Console.WriteLine($"    - Penalty: {groupResult.IndividualPenalties[i]:E4}");
    }

    // Render burning rate plots for each group
    Console.WriteLine("\nRendering burning rate plots...");
    var plotSettings = new PlotSettings
    {
        Title = "Burning Rates - Group Optimization (All Propellants)",
        TitleFontSize = 22,
        XAxisMinimum = 0.9,
        XAxisMaximum = 6.6,
        XAxisTitle = "Pressure, MPa",
        YAxisTitle = "mm/s",
        Width = 800,
        Height = 800,
        Dpi = 96
    };

    var groupPlotRenderer = new GroupBurningRatePlotRenderer();
    groupPlotRenderer.Render(groupResult, plotSettings);
    Console.WriteLine("✓ Group burning rate plot rendered\n");

    // Render Python plots
    Console.WriteLine("Rendering Python plots...");
    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper(
        "../../../../../src/python/PropellantsPlotRendering/src/main.py");
    await propellantPlotsRenderingHelper.RenderPlotsAsync("propellants.01234.json");
    Console.WriteLine("✓ Python plots rendered\n");

    // Generate PDF report
    Console.WriteLine("Generating PDF report...");
    GenerateGroupReport(groupResult!, "propellants.01234.json", "en-US", "en", propellants, deSettings, meter);
    Console.WriteLine("✓ PDF report generated\n");

    Console.WriteLine(new string('=', 90));
    Console.WriteLine("GROUP OPTIMIZATION COMPLETED SUCCESSFULLY");
    Console.WriteLine(new string('=', 90));
    Console.WriteLine("Summary:");
    Console.WriteLine("  ✓ Simultaneous optimization of 3 composition groups");
    Console.WriteLine("  ✓ 32-parameter unified vector");
    Console.WriteLine("  ✓ 11 shared parameters optimized once");
    Console.WriteLine("  ✓ 7 specific parameters for each composition");
    Console.WriteLine("  ✓ Bas_2 group includes Bas_3 and Bas_4 (same specific parameters)");
    Console.WriteLine("  ✓ Burning rate plots rendered for each composition");
    Console.WriteLine("  ✓ Python visualization plots generated");
    Console.WriteLine("  ✓ PDF report generated with group parameters");
    Console.WriteLine(new string('=', 90) + "\n");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex}");
}
