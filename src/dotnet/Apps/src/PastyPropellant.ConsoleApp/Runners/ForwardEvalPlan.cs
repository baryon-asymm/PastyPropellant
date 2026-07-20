using ParametricCombustionModel.Telemetry;
using PastyPropellant.ConsoleApp.Configuration;
using PastyPropellant.ConsoleApp.Scenarios;
using PastyPropellant.ConsoleApp.Scenarios.Settings;

namespace PastyPropellant.ConsoleApp.Runners;

/// <summary>
/// A configured single-point evaluation: the same scenario type the optimiser drives, built to solve
/// one supplied vector instead of searching.
/// </summary>
public sealed record ForwardEvalPlan
{
    /// <summary>The constructed scenario, ready to evaluate a vector.</summary>
    public required GroupDifferentialEvolutionScenario Scenario { get; init; }

    /// <summary>Scenario settings, including the DE settings echoed into the replay's report.</summary>
    public required DifferentialEvolutionScenarioSettings Settings { get; init; }

    /// <summary>Meter that times the evaluation and feeds the report's performance section.</summary>
    public required PerformanceMeter Meter { get; init; }

    /// <summary>
    /// Provenance record of this replay: the resolved configuration the vector was scored under.
    /// Written to the sidecar and summarised in the PDF report, exactly as for an optimisation run.
    /// </summary>
    public required ResolvedRunRecord Resolved { get; init; }
}
