using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using DifferentialEvolution.Optimizer.PopulationGenerators;
using ParametricCombustionModel.Core.DTOs;
using ParametricCombustionModel.Optimization.Events;
using ParametricCombustionModel.ProcessWorker.Helpers;
using ParametricCombustionModel.ProcessWorker.Scenarios;
using ParametricCombustionModel.Telemetry.GCMetricsRecorders;
using ParametricCombustionModel.Telemetry.MetricsRecorders;
using PastyPropellant.Core.IPCHelpers;
using PastyPropellant.Core.Models.Events;
using PastyPropellant.Core.Utils;

Console.WriteLine("popSize numberOfGenerations inputFileName");

EventBus<LogEvent>.Subscribe(logEvent => LogHelper.ToLog(ref logEvent));
EventBus<string>.Subscribe(msg => Console.WriteLine(msg));

var populationSize = int.Parse(args[0]);
var numberOfGenerations = int.Parse(args[1]);
var inputFileName = args[2];

const double maxPressure = 6.5e6;
const double minPressure = 1e6;

const int pressurePointsForOpt = 10;
double[] pressures = Enumerable.Range(0, pressurePointsForOpt + 1)
                               .Select(x => minPressure + (maxPressure - minPressure) / pressurePointsForOpt * x)
                               .ToArray();
const double heatFlowsSegmentSize = 100;
double[] reportingPressures = [1e6, 3.5e6, 6.5e6];
var title =
    $"Настоящий отчет сформирован на основе расчета с функциональным ограничением температуры поверхности условных топлив от 600 до 750 К, " +
    $"с параметрическим ограничением на энергию активации в газовой фазе, " +
    $"с нелинейным ограничением на плотности тепловых потоков в «кармане» (могут отстоят друг от друга не более, чем в {heatFlowsSegmentSize:#,0.0} раз), " +
    $"со «штрафом» за превышение линейной скорости горения «кармана» над скоростью горения МКМ, " +
    $"а также со «штрафом» за превышение значения плотности теплового потока кинетического пламени (1*10^8).";

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
//var experimentalBurningRates = propellants.GetExperimentalBurningRates(pressures);
//var solver = new TargetFunctionNonlconSolver(experimentalBurningRates, mixedPropellantSolvers, (600, 750));
//var constrainer = new PocketHeatFlowOffsetConstrainer(mixedPropellantSolvers, solver, 100);
//var value = constrainer.GetPenaltyValue(point);
//ReportPdfPrintScenario.GetPrintedReport(point, pressures, $"result29042024.txt", "../data/propellant_bas_1.json");

//double[] lowerBound = [1e1, 1e1, 1e1, 5e4, 1e1, 5e4, 1.3856, 1e2, 1e2, 1e1, 1e-6];
//double[] upperBound = [1e10, 1e7, 1e10, 2e5, 1e10, 2e5, 1.3856, 1e10, 1e7, 1e10, 3];
//var finder = new RandomSearchPointPool(1500, lowerBound, upperBound, 100);
//var results = await finder.GetPoolAsync();
//File.WriteAllText("results100.txt", JsonSerializer.Serialize(results));

double[] lowerBound = [1, 1, 1, 5e4, 1, 5e4, 1, 5e4, 1.3856, 1, 1, -1e12, 1e-6];
// double[] lowerBound =
// [
//     1.7665858321993308E+308, 3925354.1746398024, 2128756552.6065316, 199994.60572671256, 2321040.0404703906,
//     50000.22474985536, 880643768.4639363, 107845.10631727609, 1.3856, 4516817614.549915, 335289.81649147574,
//     -410382.28067308106, 0.6524484901077885
// ];

double[] upperBound = [double.MaxValue, 1e9, 1e12, 2e5, 1e12, 2e5, 1e12, 2e5, 1.3856, 1e12, 1e9, 1e12, 3];
// double[] upperBound =
// [
//     1.7665858321993308E+308, 3925354.1746398024, 2128756552.6065316, 199994.60572671256, 2321040.0404703906,
//     50000.22474985536, 880643768.4639363, 107845.10631727609, 1.3856, 4516817614.549915, 335289.81649147574,
//     -410382.28067308106, 0.6524484901077885
// ];

using (BinaryReader reader = new BinaryReader(File.Open($"{inputFileName}_v.bin", FileMode.Open)))
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
}

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
var populationGenerator = new PopulationGenerator(populationSize, upperBound.AsReadOnly(), lowerBound.AsReadOnly());
var scenario = new DifferentialEvolutionRuntime(
    lowerBound,
    upperBound,
    pressures,
    heatFlowsSegmentSize,
    numberOfGenerations,
    inputFileName,
    populationGenerator,
    populationGenerator
);
var individual = scenario.Run();
EventBus<string>.Publish($"Individual: {JsonSerializer.Serialize(individual)}");

using (BinaryWriter writer = new BinaryWriter(File.Open($"{inputFileName}_v.bin", FileMode.Create)))
{
    writer.Write(individual.Vector.Length);

    foreach (double value in individual.Vector.ToArray())
    {
        writer.Write(value);
    }
}

const int pressurePoints = 100000;
pressures = Enumerable.Range(0, pressurePoints + 1)
                      .Select(x => minPressure + (maxPressure - minPressure) / pressurePoints * x)
                      .ToArray();
ReportPdfPrintScenario.GetPrintedReport([title],
    individual.Vector.ToArray(),
    reportingPressures,
    pressures,
    $"{inputFileName}.pdf",
    $"../data/{inputFileName}");
Console.WriteLine("pdf generated for 100 points (diffs FFVal)");
return;
if (args.Length == 0)
{
    EventBus<LogEvent>.Publish(new LogEvent { Message = "No arguments provided.".AsMemory() });
    return;
}

var pipeName = args[0];
EventBus<LogEvent>.Publish(new LogEvent { Message = $"Pipe name: {pipeName}".AsMemory() });

var pipeClient = new NamedPipeClientStream(pipeName);

try
{
    await pipeClient.ConnectAsync(TimeSpan.FromMinutes(1), CancellationToken.None);
}
catch (Exception ex)
{
    EventBus<LogEvent>.Publish(new LogEvent { Message = $"Pipe connection failed: {ex.Message}".AsMemory() });
    return;
}

var optimizer = new MetricsGlobalSearchOptimizer();
var helper = new StreamString(pipeClient);

var workerName = await helper.ReadStringAsync();

var lastMessage = DateTime.Now;
var msgBuilder = new StringBuilder();
EventBus<OptimizationUpdatedEvent>.Subscribe(updatedEvent =>
{
    if (DateTime.Now - lastMessage < TimeSpan.FromMinutes(10) && updatedEvent.State == State.iter)
        return;

    msgBuilder.AppendLine($"Name: {workerName}");
    msgBuilder.AppendLine($"GC time {GCCounter.PauseTimePercentage} %");
    msgBuilder.AppendLine($"AllocMemory {GCCounter.TotalMemory / 1024.0 / 1024.0:#,0.000000} MB");
    msgBuilder.AppendLine($"Gen0CollCount {GCCounter.GetCollectionCount(0)}");
    msgBuilder.AppendLine($"Gen1CollCount {GCCounter.GetCollectionCount(1)}");
    msgBuilder.AppendLine($"Gen2CollCount {GCCounter.GetCollectionCount(2)}");
    msgBuilder.AppendLine(
        $"TargetFunctionCallsCount {optimizer.TargetFunctionMetricsRecorder.TargetFunctionCallsCount}");
    msgBuilder.AppendLine(
        $"MeanExecutionTime {optimizer.TargetFunctionMetricsRecorder.MeanExecutionTime:#,0.000000} ms");
    msgBuilder.AppendLine(
        $"StdDevExecutionTime {optimizer.TargetFunctionMetricsRecorder.StdDevExecutionTime:#,0.000000} ms");
    msgBuilder.AppendLine($"State: {updatedEvent.State}");
    msgBuilder.AppendLine($"StepIndex: {updatedEvent.Args.Span[0]}");
    msgBuilder.AppendLine($"FunCounts: {updatedEvent.Args.Span[1]}");

    if (updatedEvent.State == State.iter || updatedEvent.State == State.done)
    {
        msgBuilder.AppendLine($"\nBestFVal: {updatedEvent.Args.Span[2]}\n");
        for (var i = 3; i < updatedEvent.Args.Span.Length; i++)
            msgBuilder.AppendLine($"Point[{i - 3}]: {updatedEvent.Args.Span[i]}");
    }

    EventBus<LogEvent>.Publish(new LogEvent { Message = msgBuilder.ToString().AsMemory() });
    msgBuilder.Clear();
    lastMessage = DateTime.Now;
});

EventBus<LogEvent>.Publish(new LogEvent { Message = "Pipe connected".AsMemory() });

var msg = await helper.ReadStringAsync();
while (msg != StreamString.InitWorkerExit)
{
    var context = JsonSerializer.Deserialize<OptimizationContext>(msg);

    var result = await optimizer.RunAsync(context);

    if (result.IsSuccess == false)
    {
        EventBus<LogEvent>.Publish(new LogEvent
            { Message = $"Optimizer fault: {result.Exception.Message}".AsMemory() });
        return;
    }

    await helper.WriteStringAsync(StreamString.InitReceiveResult);

    var resultStr = JsonSerializer.Serialize(result.Value);
    await helper.WriteStringAsync(resultStr);

    msg = await helper.ReadStringAsync();
}
