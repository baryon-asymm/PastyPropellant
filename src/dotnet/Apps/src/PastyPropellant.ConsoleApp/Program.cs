using PastyPropellant.ConsoleApp.Workflows;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;

// Entry point. Everything below is argument dispatch: the two runnable workflows own their own
// configuration, execution and output, and the types they delegate to (BoundsProvider,
// GroupPenaltyEvaluatorFactory, GroupScenarioRunner, VectorFileService, GroupReportGenerator,
// ResultArtifactsRenderer) are reachable from the test project.

// Propellants set every run is configured against. Resolved relative to the process working
// directory by the scenario settings builder, which throws if it is not reachable from there.
const string propellantsFileName = "propellants.01234.json";

// The only flag the host parses; anything else falls through to the full optimisation run.
const string forwardEvalOption = "--forward-eval";

EventBus<InfoLogEvent>.Subscribe(logEvent => {
    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] ({logEvent.Sender ?? "none"}) | {logEvent.Message}");
});

try
{
    // Forward-eval mode: `--forward-eval <vector-file>` solves and reports a single supplied
    // group vector without running the optimizer (point evaluation; ~seconds).
    var forwardEvalIndex = Array.IndexOf(args, forwardEvalOption);
    if (forwardEvalIndex >= 0)
    {
        if (forwardEvalIndex + 1 >= args.Length)
            throw new ArgumentException(
                $"{forwardEvalOption} requires a path to a vector file, e.g. {forwardEvalOption} best_vector.txt");

        await ForwardEvalWorkflow.RunAsync(propellantsFileName, args[forwardEvalIndex + 1]);
        return;
    }

    await GroupOptimizationWorkflow.RunAsync(propellantsFileName);
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex}");
}
