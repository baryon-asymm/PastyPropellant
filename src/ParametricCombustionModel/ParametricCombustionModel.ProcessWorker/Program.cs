using System.Globalization;
using System.Text;
using System.Text.Json;
using ParametricCombustionModel.PlotRenderer.Models;
using ParametricCombustionModel.PlotRenderer.Renderers;
using ParametricCombustionModel.ProcessWorker.Helpers;
using ParametricCombustionModel.ProcessWorker.Scenarios;
using ParametricCombustionModel.ReportMaking;
using ParametricCombustionModel.ReportMaking.ReportMakers;
using PastyPropellant.Core.Models.Events;
using PastyPropellant.Core.Utils;
using PDFsharp.Api.Adapters;
using UnitsNet;

Console.WriteLine("popSize numberOfGenerations inputFileName");

EventBus<LogEvent>.Subscribe(logEvent => LogHelper.ToLog(ref logEvent));
EventBus<string>.Subscribe(msg => Console.WriteLine(msg));

var populationSize = int.Parse(args[0]);
var numberOfGenerations = int.Parse(args[1]);
var inputFileName = args[2];

var maxPressure = Pressure.FromMegapascals(6.5);
var minPressure = Pressure.FromMegapascals(1);

const int pressurePointsForOpt = 10;
const int pressurePointsForReport = 7;
var pressures = Enumerable.Range(0, pressurePointsForOpt + 1)
                          .Select(x => minPressure + (maxPressure - minPressure) / pressurePointsForOpt * x)
                          .ToArray();
var reportingPressures = Enumerable.Range(0, pressurePointsForReport + 1)
                                   .Select(x => minPressure + (maxPressure - minPressure) / pressurePointsForReport * x)
                                   .ToArray();
const double heatFlowsSegmentSize = 100;

//double[] point =
//[999999535644.0491, 123555.67822485132, 171448766466.25543, 199999.969478126, 32337545.002464958, 50418.056112171966,
//    151071153.731653, 50000.015209639096, 1.3856, 112667197989.75053, 356663.4947361756, 440457.76497882244, 0.017863561525739723];
//double[] reportingPressures = [1.2e6, 1.9e6, 2.7e6, 3.4e6, 4.1e6, 4.9e6, 5.6e6, 6.3e6, 7.1e6, 7.8e6];
//ReportPdfPrintScenario.GetPrintedReport([title], point, reportingPressures, pressures, "report.txt", "../data/propellant_bas_14.json");

//double[] point =
//[
//    2961271616.9685936, 101594.13132705934, 547864934.6380274, 101198.67023917504, 9999999331.078753, 87383.86487389341,
//    1.3856, 4937895256.869173, 270985.57133023837, 4551070.893479437, 0.013046192536326176
//];

//double[] pressures = [1.2e6, 1.9e6, 2.7e6, 3.4e6, 4.1e6, 4.9e6, 5.6e6, 6.3e6, 7.1e6, 7.8e6];

//var propellants = ReportPdfPrintScenario.GetPropellants("../data/propellant_bas_1.json");

//var mixedPropellantSolvers = MixedSolverParamsRecordersBuilder.FromPropellants(propellants)
//    .ForPressures(pressures)
//    .Build();
//var experimentalBurningRates = propellants.GetExperimentalBurnRates(pressures);
//var evaluator = new FitnessFunctionNonlconEvaluator(experimentalBurningRates, mixedPropellantSolvers, (600, 750));
//var constraintPenaltyEvaluator = new PocketHeatFluxRatioCompetitionPenaltyEvaluator(mixedPropellantSolvers, evaluator, 100);
//var value = constraintPenaltyEvaluator.GetPenaltyValue(point);
//ReportPdfPrintScenario.GetPrintedReport(point, pressures, $"result29042024.txt", "../data/propellant_bas_1.json");

//double[] lowerBound = [1e1, 1e1, 1e1, 5e4, 1e1, 5e4, 1.3856, 1e2, 1e2, 1e1, 1e-6];
//double[] upperBound = [1e10, 1e7, 1e10, 2e5, 1e10, 2e5, 1.3856, 1e10, 1e7, 1e10, 3];
//var finder = new RandomSearchPointPool(1500, lowerBound, upperBound, 100);
//var results = await finder.GetPoolAsync();
//File.WriteAllText("results100.txt", JsonSerializer.Serialize(results));

double[] lowerBound = [1, 1, 1, 5e4, 1, 5e4, 1, 5e4, 0.1, 0.1, 0.1, 1, 1, -1e12, 1e-6, 0.0];
// double[] lowerBound =
// [
//     1.7665858321993308E+308, 3925354.1746398024, 2128756552.6065316, 199994.60572671256, 2321040.0404703906,
//     50000.22474985536, 880643768.4639363, 107845.10631727609, 1.3856, 4516817614.549915, 335289.81649147574,
//     -410382.28067308106, 0.6524484901077885
// ];

double[] upperBound = [double.MaxValue, 1e9, 1e12, 2e5, 1e12, 2e5, 1e12, 2e5, 10.0, 10.0, 10.0, 1e12, 1e9, 1e12, 3, 1.0];
// double[] upperBound =
// [
//     1.7665858321993308E+308, 3925354.1746398024, 2128756552.6065316, 199994.60572671256, 2321040.0404703906,
//     50000.22474985536, 880643768.4639363, 107845.10631727609, 1.3856, 4516817614.549915, 335289.81649147574,
//     -410382.28067308106, 0.6524484901077885
// ];

/*using (BinaryReader reader = new BinaryReader(File.Open($"{inputFileName}_v.bin", FileMode.Open)))
{
    int length = reader.ReadInt32();
    double[] data = new double[length];

    for (int i = 0; i < length; i++)
    {
        data[i] = reader.ReadDouble();
    }

    Array.Copy(data, lowerBound, lowerBound.Length);
    Array.Copy(data, upperBound, upperBound.Length);

    lowerBound[0] = lowerBound[1] = 1;
    upperBound[0] = double.MaxValue;
    upperBound[1] = 1e9;
}*/

//double[] startPoint =
//[
//    999999999999.9994, 129816.4831611119, 1259911407.4294329, 199999.12958131643, 682512.7697113348, 50000,
//    2827643.282915923,
//    91095.47733578176, 1.3856, 999942663683.9297, 557540.1541231173, 217306.36019661225, 0.7929264311116686
//];
//var populationGenerator = new CustomPopulationGenerator(populationSize,
//    upperBound.AsReadOnly(),
//    lowerBound.AsReadOnly(),
//    startPoint.AsReadOnly());
/*var scenario = new DifferentialEvolutionRuntime(
    populationSize,
    numberOfGenerations,
    inputFileName,
    pressures,
    reportingPressures,
    lowerBound,
    upperBound
);
var operationResult = await scenario.RunAsync();
if (operationResult.Exception is not null)
{
    EventBus<string>.Publish(operationResult.Exception.ToString());
}*/

 var base64String =
     "hktKotOdt3t0017v4XxNQZqt8gb3lNlBuqEswBxnCEFmHuc5t8QoQU4vYQkAauhAQKIWc8kp80DLNGKP/dbvQPe2yp9JjQZA8VtaJzqP8z9J7TD9c37/P8Go+MeTLy1Bwr7r4uE6CUG8qPP5/LogwdLIMMP5/wdAXvEGlEXCaj4=";
 var byteArray = Convert.FromBase64String(base64String);
 var doubleArray = new double[byteArray.Length / sizeof(double)];
 Buffer.BlockCopy(byteArray, 0, doubleArray, 0, byteArray.Length);

var scenario = new ReportPdfPrintScenario(inputFileName, reportingPressures);
 var pressurePointCount = scenario.OptimizationProblemContext.ProblemContextMatrix.GetLength(1);
 for (int i = 0; i < pressurePointCount; i++)
 {
     scenario.OptimizationProblemContext.ProblemContextMatrix[1, i].PocketMetalCombustionParamsByUnits.MetalMeltingTemperature -= TemperatureDelta.FromKelvins(300);
     scenario.OptimizationProblemContext.ProblemContextMatrix[1, i].PropellantParamsByUnits.SkeletonSurfaceFraction += Ratio.FromDecimalFractions(0.2 / pressurePointCount * i);
 }

 var operationResult = await scenario.RunAsync(doubleArray);
 if (operationResult.Exception is not null)
 {
     EventBus<string>.Publish(operationResult.Exception.ToString());
 }

EventBus<string>.Publish(
    $"Individual: {JsonSerializer.Serialize(operationResult.Value.BestSolverParamsBySpan.ToArray())}");

var reportMaker = TextReportMaker.FromOptimizationResult(operationResult.Value);
var report = reportMaker.MakeReport();
CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
Console.OutputEncoding = Encoding.UTF8;
// Console.WriteLine(report);

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
plotRenderer.Render(operationResult.Value, settings);

var pdfGeneratorAdapter = new PdfSharpAdapter("report.pdf");
var pdfReportMaker = PdfReportMaker.FromOptimizationResult(operationResult.Value, pdfGeneratorAdapter);
pdfReportMaker.MakeReport();

// using (BinaryWriter writer = new BinaryWriter(File.Open($"{inputFileName}_v.bin", FileMode.Create)))
// {
//     writer.Write(individual.Vector.Length);
//
//     foreach (double value in individual.Vector.ToArray())
//     {
//         writer.Write(value);
//     }
// }

return;
const int pressurePoints = 100000;
pressures = Enumerable.Range(0, pressurePoints + 1)
                      .Select(x => minPressure + (maxPressure - minPressure) / pressurePoints * x)
                      .ToArray();
// // ReportPdfPrintScenario.GetPrintedReport([title],
// //                                         individual.Vector.ToArray(),
// //                                         reportingPressures,
// //                                         pressures,
// //                                         $"{inputFileName}.pdf",
//                                         $"../data/{inputFileName}");
Console.WriteLine("pdf generated for 100 points (diffs FFVal)");
