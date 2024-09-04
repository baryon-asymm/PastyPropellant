using PastyPropellant.ConsoleApp;
using PastyPropellant.Core.Models.Events;
using PastyPropellant.Core.Utils;

EventBus<LogEvent>.Publish(new LogEvent { Message = "PastyPropellant.ConsoleApp starting...".AsMemory() });

var initializer = new Initializer("data/optimization_tickets.json", "data/telegram_settings.json");
var operationResult = await initializer.InitializeAsync();

EventBus<LogEvent>.Publish(new LogEvent { Message = "PastyPropellant.ConsoleApp initialized.".AsMemory() });

if (operationResult.IsSuccess) operationResult = await initializer.RunAsync();

if (operationResult.IsSuccess == false)
    EventBus<LogEvent>.Publish(new LogEvent { Message = operationResult.Exception.Message.AsMemory() });

EventBus<LogEvent>.Publish(new LogEvent { Message = "PastyPropellant.ConsoleApp execution completed.".AsMemory() });
Console.ReadKey();

/*
Func<double[], Task<OperationResult<OptimizationResult>>> compute = async (point) =>
{
    DateTime lastMessage = DateTime.Now;
    Func<double[], State, bool> onUpdateCallback = (args, state) =>
    {
        if (DateTime.Now - lastMessage < TimeSpan.FromMinutes(10) && state == State.iter)
            return false;

        string message = $"Name: {name}\nState: {state}\nStepIndex: {args[0]}\nFunCounts: {args[1]}";
        if (state == State.iter || state == State.done)
        {
            message += $"\nBestFVal: {args[2]}\n\n";
            for (int i = 3; i < args.Length; i++)
            {
                message += $"Point[{i - 3}]: {args[i]}\n";
            }
        }
        Console.WriteLine(message);
        telegramNotifier.NotifyAsync(message).Wait();
        lastMessage = DateTime.Now;
        return false;
    };

    result = await optimizer.RunAsync(new OptimizationContext(pressures, point, trialPointsCount, numStageOnePointsCount, lowerBound, upperBound, (600, 750), propellants, onUpdateCallback));

    return result;
};

await Parallel.ForEachAsync(points, async (point, token) =>
{
    Console.WriteLine("Start worker");



    double[] temppoint = point;
    OperationResult<OptimizationResult> result = null;
    for (int i = 0; i < 3; i++)
    {
        result = await compute(temppoint);
        temppoint = result.Value.FinalPoint.ToArray();
        Console.WriteLine(result.Value.TargetFunctionValue);
    }

    var reportMaker = new ReportMaker();
    var reportOperation = await reportMaker.GetReportAsync(new ReportMakingContext(pressures, result.Value.FinalPoint, propellants));

    if (reportOperation.IsSuccess)
    {
        var report = reportOperation.Value;
        var printer = new ParametricModelReportMaker();
        Console.OutputEncoding = Encoding.UTF8;
        File.WriteAllText($"report-{Guid.NewGuid()}.txt", printer.MakeReport(result.Value.FinalPoint, report), Encoding.UTF8);
        Console.WriteLine(printer.MakeReport(result.Value.FinalPoint, report));
    }
});

return;

var reader = new Test();

var operationResult = await ThermodynamicTicketsControllerBuilder
    .Create()
    .WithTicketReader(reader)
    .WithSubstanceReader(new BaseFileModelReader<List<ThermodynamicSubstance>>("thermodynamic_substances.json"))
    .BuildAsync();

if (operationResult.IsSuccess)
{
    var controller = operationResult.Value;
    controller.Run();
}

class Test : IModelReader<List<ThermodynamicTicket>>
{
    public Task<OperationResult<List<ThermodynamicTicket>>> ReadAllAsync()
    {
        return Task.FromResult(
            new OperationResult<List<ThermodynamicTicket>>(
                new List<ThermodynamicTicket>()
                {
                    new ThermodynamicTicket("Test", "C 8.8268  H 32.7980  O 22.5185  N 12.4537  Cl 3.4060  Al 7.6830")
                }
            )
        );
    }
}

//var finder = new CombustionProductsFinder();

/*
 * var fileWriter = new FileStream("thermodynamic_substances.json", FileMode.Create);
var items = new List<ThermodynamicSubstance>();

var file = File.Open("C:\\Users\\edwar\\Downloads\\TAB\\TAB.dat", FileMode.Open);
var reader = new StreamReader(file);
string? line = String.Empty;
while ((line = reader.ReadLine()) != null)
{
    RegexOptions options = RegexOptions.None;
    Regex regex = new Regex("[ ]{2,}", options);
    var sentence = regex.Replace(line.Replace('\'', ' '), " ");
    var strs = sentence.Trim().Replace('.', ',').Split(' ');
    var coeffs = new List<double>();
    for (int i = 1; i < strs.Length; i++)
    {
        coeffs.Add(double.Parse(strs[i]));
    }

    items.Add(new ThermodynamicSubstance(strs[0], coeffs, SubstancePhase.Gas, new TemperatureRange(1000.0, 5000.0)));
}

await JsonSerializer.SerializeAsync(fileWriter, items);
 */
