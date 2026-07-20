using PastyPropellant.ConsoleApp.Configuration;
using PastyPropellant.ConsoleApp.Workflows;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;

// Entry point. Everything below is argument dispatch: the two runnable workflows own their own
// execution and output, the run's numerical configuration is resolved once by RunConfigurationLoader,
// and the types they delegate to (BoundsProvider, GroupPenaltyEvaluatorFactory, GroupScenarioRunner,
// VectorFileService, GroupReportGenerator, ResultArtifactsRenderer) are reachable from the test project.

// The flags the host parses; anything else falls through to the full optimisation run.
const string forwardEvalOption = "--forward-eval";
const string configOption = "--config";

EventBus<InfoLogEvent>.Subscribe(logEvent => {
    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] ({logEvent.Sender ?? "none"}) | {logEvent.Message}");
});

try
{
    // Run configuration: OPTIONAL. With no --config and no run-config.json in the working directory the
    // loader returns the built-in defaults, which are the literals this host used to have compiled in —
    // so an unconfigured run is byte-for-byte the pre-configuration run. A supplied file is mandatory
    // and is validated before anything is constructed; whatever is resolved gets recorded in
    // run_configuration.resolved.json and in the PDF report, which is what keeps a run traceable now
    // that it no longer requires a recompile.
    var loadedConfiguration = RunConfigurationLoader.Load(GetOptionValue(configOption));

    // Forward-eval mode: `--forward-eval <vector-file>` solves and reports a single supplied
    // group vector without running the optimizer (point evaluation; ~seconds).
    var forwardEvalVectorPath = GetOptionValue(forwardEvalOption);
    if (forwardEvalVectorPath != null)
    {
        await ForwardEvalWorkflow.RunAsync(loadedConfiguration, forwardEvalVectorPath);
        return;
    }

    await GroupOptimizationWorkflow.RunAsync(loadedConfiguration);
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex}");
}

// Returns the value following `option`, or null when the option is absent. A present option with no
// value is an error rather than a silent no-op — a mistyped command line must not run a different
// configuration than the operator asked for.
string? GetOptionValue(string option)
{
    var index = Array.IndexOf(args, option);
    if (index < 0)
        return null;

    if (index + 1 >= args.Length)
        throw new ArgumentException(
            $"{option} requires a value, e.g. {option} " +
            (option == forwardEvalOption ? "best_vector.txt" : RunConfigurationLoader.DefaultFileName));

    return args[index + 1];
}
