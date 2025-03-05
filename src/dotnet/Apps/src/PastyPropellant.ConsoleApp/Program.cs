
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

await RunPreparingScenarioAsync();
