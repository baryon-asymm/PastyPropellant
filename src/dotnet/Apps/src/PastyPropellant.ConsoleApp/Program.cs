using System.Collections.ObjectModel;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;
using ParametricCombustionModel.ProcessWorker.Scenarios;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PastyPropellant.Thermodynamics.Calculators;
using PDFsharp.Api.Adapters;
using UnitsNet;

async Task RunDEScenarioAsync(int scenarioNumber, double poreDiameterThreshold, double nuPocketSkeletonMin, double nuPocketSkeletonMax)
{
    // Base parameter bounds
    double[] lowerBound = [
        1,      // ADecompose (from calculation)
        1,      // EDecompose (from calculation)
        3.26e+09,      // AKineticFlameInterPocket (from calculation)
        199999.99,     // EKineticFlameInterPocket (from calculation)
        2.62e+06,      // AKineticFlamePocketOutSkeleton (from calculation)
        50000,         // EKineticFlamePocketOutSkeleton (from calculation)
        1,             // AKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        1.0,           // EKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        2.2658901079487093,  // NuInterPocket (fixed from calculation)
        1.0000000000060636,   // NuPocketOutSkeleton (fixed from calculation)
        2.149641911533772,  // NuPocketSkeleton (will be adjusted per scenario)
        0.0,      // AMetalBurningConstant (from calculation)
        0.0,      // BMetalBurningConstant (from calculation)
        -360438.99,    // DeltaH (from calculation)
        0.9961,        // KDiffusionHeight (from calculation)
        1.0,           // APowOrder (from calculation)
        2.0,           // BPowOrder (from calculation)
        0.0            // KCoefficientRadiationTemperature (from calculation)
    ];

    double[] upperBound = [
        double.MaxValue,      // ADecompose (fixed from calculation)
        1e9,      // EDecompose (fixed from calculation)
        3.26e+09,      // AKineticFlameInterPocket (fixed from calculation)
        199999.99,     // EKineticFlameInterPocket (fixed from calculation)
        2.62e+06,      // AKineticFlamePocketOutSkeleton (fixed from calculation)
        50000,         // EKineticFlamePocketOutSkeleton (fixed from calculation)
        1e12,          // AKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        2e5,           // EKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        2.2658901079487093,  // NuInterPocket (fixed from calculation)
        1.0000000000060636,   // NuPocketOutSkeleton (fixed from calculation)
        2.149641911533772,  // NuPocketSkeleton (will be adjusted per scenario)
        1.0,      // AMetalBurningConstant (fixed from calculation)
        1.0,      // BMetalBurningConstant (fixed from calculation)
        -360438.99,    // DeltaH (fixed from calculation)
        0.9961,        // KDiffusionHeight (fixed from calculation)
        1.0,           // APowOrder (fixed from calculation)
        2.0,           // BPowOrder (fixed from calculation)
        0.0            // KCoefficientRadiationTemperature (fixed from calculation)
    ];

    // Adjust NuPocketSkeleton bounds based on scenario
    lowerBound[10] = nuPocketSkeletonMin;
    upperBound[10] = nuPocketSkeletonMax;

    const string inputFileName = "propellants.json"; // Единый входной файл для всех сценариев
    var scenario = new DifferentialEvolutionRuntime(
        populationSize: lowerBound.Length * 8,
        maxStagnationStreak: 100_000,
        inputFileName,
        lowerBound: lowerBound,
        upperBound: upperBound,
        poreDiameterThreshold: poreDiameterThreshold
    );

    Console.WriteLine($"Starting scenario {scenarioNumber} with parameters:");
    Console.WriteLine($"- Pore diameter threshold: {poreDiameterThreshold}");
    Console.WriteLine($"- NuPocketSkeleton range: {nuPocketSkeletonMin} to {nuPocketSkeletonMax}");

    var operationResult = await scenario.RunAsync();
    if (operationResult.IsSuccess == false)
    {
        Console.WriteLine($"Scenario {scenarioNumber} failed:");
        Console.WriteLine(operationResult.Exception);
        return;
    }

    var settings = new PlotSettings
    {
        Title = $"Burning Rates - Scenario {scenarioNumber}",
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

    // Генерация уникальных имен файлов для каждого сценария
    var plotsOutputPrefix = $"scenario_{scenarioNumber}_";
    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper(
        "../../../../../src/python/PropellantsPlotRendering/src/main.py");
    
    var plotsResult = await propellantPlotsRenderingHelper.RenderPlotsAsync(inputFileName);

    var pdfOutputFileName = $"propellants.scenario_{scenarioNumber}.report.pdf";
    var pdfGeneratorAdapter = new PdfSharpAdapter(pdfOutputFileName);
    if (operationResult.Value is not null)
    {
        var pdfReportMaker = PdfReportMaker.FromOptimizationResult(operationResult.Value, pdfGeneratorAdapter);
        pdfReportMaker.MakeReport();
    }

    Console.WriteLine($"Scenario {scenarioNumber} completed successfully");
    Console.WriteLine($"Results saved with prefix 'scenario_{scenarioNumber}_'");
}

async Task RunAllScenariosAsync()
{
    // Scenario 1: Original bounds, pore diameter threshold 3.0
    await RunDEScenarioAsync(1, 3.0, 2.149641911533772, 2.149641911533772);
    
    // Scenario 2: Original bounds, pore diameter threshold 100.0
    await RunDEScenarioAsync(2, 100.0, 2.149641911533772, 2.149641911533772);
    
    // Scenario 3: NuPocketSkeleton 1.0-10.0, pore diameter threshold 3.0
    await RunDEScenarioAsync(3, 3.0, 1.0, 10.0);
    
    // Scenario 4: NuPocketSkeleton 1.0-10.0, pore diameter threshold 100.0
    await RunDEScenarioAsync(4, 100.0, 1.0, 10.0);
}

async Task RunThermodynamicsScenarioAsync()
{
    const string propellantsFilePath = "propellants.234.json";
    const string componentsFilePath = "../../../../../data/propellant_components.json";
    const string combustionProductsFilePath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json";
    const string artifactDirectoryPath = "../../../../../artifacts";
    
    const string pyMapperScriptPath = "../../../../../src/python/RegionMapper/src/main.py";
    const string pyThermodynamicsScriptPath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/src/main.py";
    const string pyPorosityScriptPath = "../../../../../src/python/PorosityCalculation/src/main.py";
    
    const string outputConstructedPropellantFilePath = "propellants.234.with_tdc.json";
    
    var pressures = GetPressures();
    
    Console.WriteLine("Starting comprehensive thermodynamics scenario...");
    Console.WriteLine($"Input files:");
    Console.WriteLine($"  - Propellants: {propellantsFilePath}");
    Console.WriteLine($"  - Components: {componentsFilePath}");
    Console.WriteLine($"  - Combustion products: {combustionProductsFilePath}");
    Console.WriteLine($"Pressure points: {pressures.Count}");
    Console.WriteLine($"Artifact directory: {artifactDirectoryPath}");
    Console.WriteLine();
    
    // Step 1: Prepare propellant data using PreparePropellantDataHelper
    Console.WriteLine("Step 1: Preparing propellant data (mapping, thermodynamics, porosity)...");
    
    var preparePropellantDataHelper = new PreparePropellantDataHelper(
        artifactDirectoryPath,
        pyMapperScriptPath,
        pyThermodynamicsScriptPath,
        pyPorosityScriptPath,
        pressures
    );
    
    var prepareResult = await preparePropellantDataHelper.PrepareAsync(
        propellantsFilePath, componentsFilePath, combustionProductsFilePath);
    
    if (prepareResult.IsSuccess == false)
    {
        Console.WriteLine($"Failed to prepare propellant data: {prepareResult.Exception?.Message}");
        return;
    }
    
    Console.WriteLine($"Successfully prepared data for {prepareResult.Value!.Count} propellants");
    Console.WriteLine();
    
    // Step 2: Construct final JSON using ConstructPropellantJsonHelper
    Console.WriteLine("Step 2: Constructing final propellant JSON...");
    
    var constructHelper = new ConstructPropellantJsonHelper(propellantsFilePath, prepareResult.Value);
    var constructResult = await constructHelper.ConstructAsync(outputConstructedPropellantFilePath);
    
    if (constructResult.IsSuccess == false)
    {
        Console.WriteLine($"Failed to construct propellant JSON: {constructResult.Exception?.Message}");
        return;
    }
    
    Console.WriteLine($"Successfully constructed propellant JSON: {outputConstructedPropellantFilePath}");
    Console.WriteLine();
    
    // Step 3: Display summary
    Console.WriteLine("Thermodynamics scenario completed successfully!");
    Console.WriteLine("Summary:");
    foreach (var propellantData in prepareResult.Value)
    {
        Console.WriteLine($"  - Propellant: {propellantData.Name}");
        Console.WriteLine($"    Thermodynamics files: {propellantData.PressureFrameThermodynamics.Count}");
        Console.WriteLine($"    Porosity files: {propellantData.PorosityPropellants.Count}");
    }
    Console.WriteLine($"Final output: {outputConstructedPropellantFilePath}");
}

async Task RunPointCalculationAsync()
{
    double[] lowerBound = [1, 1, 1, 5e4, 1, 5e4, 1, 5e4, 1.0, 1.0, 1.0, 9.73e-06, 2.35e-08, -1e12, 1e-6, 1.0, 2.0, 0.0];
    double[] upperBound = [double.MaxValue, 1e9, 1e12, 2e5, 1e12, 2e5, 1e12, 2e5, 10.0, 10.0, 10.0, 9.73e-06, 2.35e-08, 1e12, 10, 1.0, 2.0, 0.0];
    
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

    const string inputFileName = "propellant_bas_0.json";
    const double poreDiameterThreshold = 3.0; // Значение по умолчанию
    
    var scenario = new DifferentialEvolutionRuntime(
        populationSize: lowerBound.Length * 8,
        maxStagnationStreak: 100_000,
        inputFileName,
        lowerBound: lowerBound,
        upperBound: upperBound,
        poreDiameterThreshold: poreDiameterThreshold
    );

    Console.WriteLine("Starting point calculation with custom bounds:");
    Console.WriteLine($"- Input file: {inputFileName}");
    Console.WriteLine($"- Pore diameter threshold: {poreDiameterThreshold}");
    Console.WriteLine($"- Population size: {lowerBound.Length * 8}");

    var operationResult = await scenario.RunAsync();
    if (operationResult.IsSuccess == false)
    {
        Console.WriteLine("Point calculation failed:");
        Console.WriteLine(operationResult.Exception);
        return;
    }

    var settings = new PlotSettings
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
    if (operationResult.Value is not null)
    {
        plotRenderer.Render(operationResult.Value, settings);
    }

    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper(
        "../../../../../src/python/PropellantsPlotRendering/src/main.py");
    
    var plotsResult = await propellantPlotsRenderingHelper.RenderPlotsAsync(inputFileName);

    const string pdfOutputFileName = "propellants.point_calculation.report.pdf";
    var pdfGeneratorAdapter = new PdfSharpAdapter(pdfOutputFileName);
    if (operationResult.Value is not null)
    {
        var pdfReportMaker = PdfReportMaker.FromOptimizationResult(operationResult.Value, pdfGeneratorAdapter);
        pdfReportMaker.MakeReport();
    }

    Console.WriteLine("Point calculation completed successfully");
    Console.WriteLine($"Results saved as: {pdfOutputFileName}");
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
    
    // await RunAllScenariosAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
