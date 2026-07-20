using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.Telemetry;
using PastyPropellant.ConsoleApp.Configuration;
using PastyPropellant.ConsoleApp.Scenarios;
using PastyPropellant.ConsoleApp.Scenarios.Settings;

namespace PastyPropellant.ConsoleApp.Runners;

/// <summary>
/// A fully configured, not-yet-started group optimisation run: the built scenario plus the derived
/// values the host reports before starting it.
///
/// <para>The derived values are carried explicitly rather than recomputed by the caller. Population
/// size and worker count are the product of a strategy-dependent branch (see
/// <see cref="GroupScenarioRunner.CreateOptimizationPlan"/>), so a banner that recalculated them could
/// print numbers the optimiser is not actually using — the classic way a run log stops matching the
/// run.</para>
/// </summary>
public sealed record GroupOptimizationPlan
{
    /// <summary>The constructed scenario, ready to run.</summary>
    public required GroupDifferentialEvolutionScenario Scenario { get; init; }

    /// <summary>Scenario settings, including the underlying DE settings recorded in the report.</summary>
    public required DifferentialEvolutionScenarioSettings Settings { get; init; }

    /// <summary>Meter that times the run and feeds the report's performance section.</summary>
    public required PerformanceMeter Meter { get; init; }

    /// <summary>Population size the strategy branch selected.</summary>
    public required int PopulationSize { get; init; }

    /// <summary>In-process DE worker count, each with its own problem-context copy.</summary>
    public required int ProcessorsCount { get; init; }

    /// <summary>Evaluation budget, set only for the budget-driven L-SHADE variant.</summary>
    public long? MaxEvaluationNumber { get; init; }

    /// <summary>L-SHADE p-best rate; null for the fixed-population strategies, which ignore it.</summary>
    public double? PBestRate { get; init; }

    /// <summary>L-SHADE archive size rate; null for the fixed-population strategies.</summary>
    public double? ArchiveSizeRate { get; init; }

    /// <summary>L-SHADE memory size; null for the fixed-population strategies.</summary>
    public int? MemorySize { get; init; }

    /// <summary>Nelder–Mead refinement configuration layered on the DE search.</summary>
    public required NelderMeadRefinementSettings NelderMeadRefinement { get; init; }

    /// <summary>
    /// Provenance record of this run: the resolved configuration plus the effective values derived from
    /// it. Written to the sidecar and summarised in the PDF report.
    /// </summary>
    public required ResolvedRunRecord Resolved { get; init; }
}
