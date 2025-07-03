using System.Collections.ObjectModel;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;
using ParametricCombustionModel.ProcessWorker.Scenarios;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PDFsharp.Api.Adapters;
using UnitsNet;

async Task RunDEScenarioAsync()
{
    double[] lowerBound = [1, 1, 1, 5e4, 1, 5e4, 1, 5e4, 1.0, 1.0, 1.0, 0, 0, -1e12, 1e-6, 1.0, 2.0, 0.0];
    double[] upperBound = [double.MaxValue, 1e9, 1e12, 2e5, 1e12, 2e5, 1e12, 2e5, 10.0, 10.0, 10.0, 1, 1, 1e12, 10, 1.0, 2.0, 1.0];

    lowerBound = [
        1,      // ADecompose (from calculation)
        1,      // EDecompose (from calculation)
        3.26e+09,      // AKineticFlameInterPocket (from calculation)
        199999.99,     // EKineticFlameInterPocket (from calculation)
        2.62e+06,      // AKineticFlamePocketOutSkeleton (from calculation)
        50000,         // EKineticFlamePocketOutSkeleton (from calculation)
        1,             // AKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        1.0,           // EKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        1.0,  // NuInterPocket (fixed from calculation)
        1.0000000000060636,   // NuPocketOutSkeleton (fixed from calculation)
        2.149641911533772,  // NuPocketSkeleton (RESTORED original value)
        0.0,      // AMetalBurningConstant (from calculation)
        0.0,      // BMetalBurningConstant (from calculation)
        -360438.99,    // DeltaH (from calculation)
        0.9961,        // KDiffusionHeight (from calculation)
        1.0,           // APowOrder (from calculation)
        2.0,           // BPowOrder (from calculation)
        0.0            // KCoefficientRadiationTemperature (from calculation)
        ];

    upperBound = [
        double.MaxValue,      // ADecompose (fixed from calculation)
        1e9,      // EDecompose (fixed from calculation)
        3.26e+09,      // AKineticFlameInterPocket (fixed from calculation)
        199999.99,     // EKineticFlameInterPocket (fixed from calculation)
        2.62e+06,      // AKineticFlamePocketOutSkeleton (fixed from calculation)
        50000,         // EKineticFlamePocketOutSkeleton (fixed from calculation)
        1e12,          // AKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        2e5,           // EKineticFlamePocketSkeleton (UNCHANGED skeleton param)
        10.0,  // NuInterPocket (fixed from calculation)
        1.0000000000060636,   // NuPocketOutSkeleton (fixed from calculation)
        2.149641911533772,  // NuPocketSkeleton (RESTORED original value)
        1.0,      // AMetalBurningConstant (fixed from calculation)
        1.0,      // BMetalBurningConstant (fixed from calculation)
        -360438.99,    // DeltaH (fixed from calculation)
        0.9961,        // KDiffusionHeight (fixed from calculation)
        1.0,           // APowOrder (fixed from calculation)
        2.0,           // BPowOrder (fixed from calculation)
        0.0            // KCoefficientRadiationTemperature (fixed from calculation)
        ];

    // Skeleton checking
    //lowerBound = [2.32e+08, 87508.8, 4.88e+09, 199999.99, 3.25e+06, 50000, 1, 1.0, 2.4321891826017246, 1.0, 1.0000000000263074, 9.27e-08, 4.55e-07, -348655.79, 1.4265, 0.0, 1.0, 2.0];
    //upperBound = [2.32e+08, 87508.8, 4.88e+09, 199999.99, 3.25e+06, 50000, 1e12, 2e5, 2.4321891826017246, 10.0, 1.0000000000263074, 9.27e-08, 4.55e-07, -348655.79, 1.4265, 0.0, 1.0, 2.0];

    /*var inputFileName = "propellants.prev.json"; // 1234
    var scenario = new DifferentialEvolutionRuntime(
        populationSize: lowerBound.Length * 8,
        maxStagnationStreak: 100_000,
        inputFileName,
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

    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper("../../../../../src/python/PropellantsPlotRendering/src/main.py");
    var plotsResult = await propellantPlotsRenderingHelper.RenderPlotsAsync(inputFileName);

    var pdfGeneratorAdapter = new PdfSharpAdapter($"{inputFileName}.report.pdf");
    if (operationResult.Value is not null)
    {
        var pdfReportMaker = PdfReportMaker.FromOptimizationResult(operationResult.Value, pdfGeneratorAdapter);
        pdfReportMaker.MakeReport();
    }*/

    // 234
    var inputFileName = "propellants.json"; // 234
    var scenario = new DifferentialEvolutionRuntime(
        populationSize: lowerBound.Length * 8,
        maxStagnationStreak: 100_000,
        inputFileName, // 234
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

    var propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper("../../../../../src/python/PropellantsPlotRendering/src/main.py");
    var plotsResult = await propellantPlotsRenderingHelper.RenderPlotsAsync(inputFileName);

    var pdfGeneratorAdapter = new PdfSharpAdapter($"{inputFileName}.report.pdf");
    if (operationResult.Value is not null)
    {
        var pdfReportMaker = PdfReportMaker.FromOptimizationResult(operationResult.Value, pdfGeneratorAdapter);
        pdfReportMaker.MakeReport();
    }

    // 1 from result for 234
    /*var lastOptimizationResult = operationResult.Value!;
    lowerBound = lastOptimizationResult.BestSolverParamsBySpan.ToArray();
    lowerBound[6] = 1.0;
    lowerBound[7] = 1.0;
    lowerBound[9] = 1.0;
    upperBound = lastOptimizationResult.BestSolverParamsBySpan.ToArray();
    upperBound[6] = 1e12;
    upperBound[7] = 2e5;
    upperBound[9] = 10.0;
    inputFileName = "propellants.json"; // 1
    scenario = new DifferentialEvolutionRuntime(
        populationSize: lowerBound.Length * 8,
        maxStagnationStreak: 100_000,
        inputFileName, // 1
        lowerBound: lowerBound,
        upperBound: upperBound
    );
    operationResult = await scenario.RunAsync();
    if (operationResult.IsSuccess == false)
    {
        Console.WriteLine(operationResult.Exception);
        return;
    }

    settings = new PlotSettings
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
    plotRenderer = new BurningRatePlotRenderer();
    if (operationResult.Value is not null)
    {
        plotRenderer.Render(operationResult.Value, settings);
    }

    propellantPlotsRenderingHelper = new PropellantPlotsRenderingHelper("../../../../../src/python/PropellantsPlotRendering/src/main.py");
    plotsResult = await propellantPlotsRenderingHelper.RenderPlotsAsync(inputFileName);

    pdfGeneratorAdapter = new PdfSharpAdapter($"{inputFileName}.report.pdf");
    if (operationResult.Value is not null)
    {
        var pdfReportMaker = PdfReportMaker.FromOptimizationResult(operationResult.Value, pdfGeneratorAdapter);
        pdfReportMaker.MakeReport();
    }*/
}

async Task RunPreparingScenarioAsync()
{
    var artifactDirectoryPath = "../../../../../artifacts/output";
    var pyMapperScriptPath = "../../../../../src/python/RegionMapper/src/main.py";
    var pyThermodynamicsScriptPath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/src/main.py";
    var pyPorosityScriptPath = "../../../../../src/python/PorosityCalculation/src/main.py";
    var propellantsFilePath = "../../../../../data/propellants.json";
    var componentsFilePath = "../../../../../src/python/RegionMapper/data/components.json";
    var combustionProductsFilePath = "../../../../../externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json";
    var outputPropellantsFilePath = Path.Combine(artifactDirectoryPath, "propellants.json");

    var pressures = GetPressures();
    var preparePropellantHelper = new PreparePropellantDataHelper(
        artifactDirectoryPath, pyMapperScriptPath, pyThermodynamicsScriptPath, pyPorosityScriptPath, pressures);
    
    var result = await preparePropellantHelper.PrepareAsync(
        propellantsFilePath, componentsFilePath, combustionProductsFilePath);
    if (result.IsSuccess == false)
    {
        EventBus<InfoLogEvent>.Publish(
            new InfoLogEvent($"Preparing scenario failed\n{result.Exception}", "Program"));
        return;
    }

    var constructPropellantJsonHelper = new ConstructPropellantJsonHelper(propellantsFilePath, result.Value!);
    var constructResult = await constructPropellantJsonHelper.ConstructAsync(outputPropellantsFilePath);
    if (constructResult.IsSuccess == false)
    {
        EventBus<InfoLogEvent>.Publish(
            new InfoLogEvent($"Constructing scenario failed\n{constructResult.Exception}", "Program"));
        return;
    }

    EventBus<InfoLogEvent>.Publish(new InfoLogEvent("Preparing scenario completed", "Program"));
}

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
    await RunDEScenarioAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
