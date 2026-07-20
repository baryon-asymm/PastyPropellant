using DotNetDifferentialEvolution.TerminationStrategies;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
using ParametricCombustionModel.Telemetry;
using PastyPropellant.ConsoleApp.Configuration;
using PastyPropellant.ConsoleApp.Scenarios;
using PastyPropellant.ConsoleApp.Scenarios.Settings;
using PastyPropellant.Core.Utils;

namespace PastyPropellant.ConsoleApp.Runners;

/// <summary>
/// Configures and executes the group differential-evolution scenario. This is where the run's
/// numerical configuration lives — search strategy, population sizing, termination, Nelder–Mead
/// refinement — and it is deliberately free of console output so the configuration can be asserted on
/// in a test without capturing stdout. The host narrates; this type decides.
///
/// <para>Both entry points are here for one reason: the optimisation run and the <c>--forward-eval</c>
/// replay must be the same code path. They take their bounds from <see cref="BoundsProvider"/> and
/// their penalties from <see cref="GroupPenaltyEvaluatorFactory"/>, so a replayed vector is scored by
/// exactly the configuration that produced it. Forward-eval differs only where it provably cannot
/// matter — it never runs the search, so its population size, strategy and termination strategy are
/// builder-required placeholders that no evaluation reads.</para>
/// </summary>
public static class GroupScenarioRunner
{
    /// <summary>
    /// Builds the production optimisation run from the run configuration: bounds, penalties, strategy,
    /// population, termination and Nelder–Mead refinement, plus the scenario itself.
    /// </summary>
    /// <param name="loadedConfiguration">
    /// The resolved run configuration and its provenance. Pass
    /// <see cref="RunConfigurationLoader.Load"/>'s result; the defaults reproduce the historical run.
    /// </param>
    public static GroupOptimizationPlan CreateOptimizationPlan(LoadedRunConfiguration loadedConfiguration)
    {
        ArgumentNullException.ThrowIfNull(loadedConfiguration);

        var configuration = loadedConfiguration.Configuration;
        var deConfiguration = configuration.DifferentialEvolution;
        var inputFileName = configuration.InputFileName;

        var meter = new PerformanceMeter();

        var penaltyEvaluators = GroupPenaltyEvaluatorFactory.Build(configuration.Penalties);

        var groupLowerBound = configuration.Bounds.ToGroupLowerBound();
        var groupUpperBound = configuration.Bounds.ToGroupUpperBound();
        var dimensions = groupLowerBound.Length; // 32

        // jDE (self-adaptive rand/1) is the winning strategy on this branch and remains the configured
        // default: its exploration reaches the clean basin (obj 0.157 / penalty 0), whereas current-to-pbest
        // variants either trap in a penalized degenerate corner (SHADE/L-SHADE → 0.92) or converge
        // prematurely to a worse optimum (JADE → 0.42).
        var strategy = deConfiguration.Strategy;
        var maxAvailableProcessors = Math.Max(1, Environment.ProcessorCount - 1);
        var safetyTimeout = new TimeoutTerminationStrategy(TimeSpan.FromHours(deConfiguration.SafetyTimeoutHours));

        int populationSize;
        int processorsCount;
        OrTerminationStrategy terminationStrategy;
        string terminationDescription;
        long? maxEvaluationNumber = null;
        // L-SHADE control parameters (left null for the fixed-population variants, which ignore them).
        double? pBestRate = null;
        double? archiveSizeRate = null;
        int? memorySize = null;

        if (strategy == DifferentialEvolutionStrategy.LShade)
        {
            // L-SHADE: a larger initial population (18*D) that shrinks linearly to ~4 across a fixed
            // evaluation budget; the budget both terminates the run and defines the reduction schedule.
            // Size it to what is actually achievable so the 576→4 schedule completes — a huge budget just
            // back-loads convergence (the 9 h timeout would fire long before LPSR reaches the floor).
            // NOTE: the memetic Nelder–Mead evaluations count toward this same budget.
            var lShade = deConfiguration.LShade;
            populationSize = dimensions * lShade.PopulationSizeMultiplier; // 32 * 18 = 576
            processorsCount = maxAvailableProcessors; // population shrinks, so the divisibility heuristic does not apply
            maxEvaluationNumber = lShade.MaxEvaluationNumber;
            // Canonical L-SHADE control parameters (Tanabe & Fukunaga 2014): p-best 0.11, archive rate 2.6, memory 6.
            pBestRate = lShade.PBestRate;
            archiveSizeRate = lShade.ArchiveSizeRate;
            memorySize = lShade.MemorySize;
            terminationStrategy = new OrTerminationStrategy(
                new LimitEvaluationNumberTerminationStrategy(maxEvaluationNumber.Value),
                safetyTimeout);
            terminationDescription =
                $"evaluation budget {maxEvaluationNumber.Value:N0} OR {deConfiguration.SafetyTimeoutHours:0.###} h safety timeout";
        }
        else
        {
            // Fixed-population variants (Classic / jDE / JADE / SHADE): keep the stagnation+timeout
            // run control and pick a worker count that divides the population evenly for balanced load.
            var fixedPopulation = deConfiguration.FixedPopulation;
            populationSize = dimensions * fixedPopulation.PopulationSizeMultiplier; // 32 * 12 = 384 (jDE production population — reaches obj 0.157 / penalty 0)
            processorsCount = maxAvailableProcessors;
            for (; processorsCount >= fixedPopulation.MinProcessorsCount; processorsCount--)
                if (populationSize % processorsCount == 0)
                    break;
            terminationStrategy = new OrTerminationStrategy(
                new CustomStagnationStreakTerminationStrategy(
                    maxStagnationStreak: fixedPopulation.MaxStagnationStreak,
                    relativeStagnationThreshold: fixedPopulation.RelativeStagnationThreshold),
                safetyTimeout);
            terminationDescription =
                $"stagnation streak {fixedPopulation.MaxStagnationStreak:N0} @ rel {fixedPopulation.RelativeStagnationThreshold:G3} " +
                $"OR {deConfiguration.SafetyTimeoutHours:0.###} h safety timeout";
        }

        // Nelder–Mead refinement layered on top of the DE search (DotNetNelderMead.DifferentialEvolution):
        // an in-loop memetic refiner every N generations plus a final sequential polish of the converged best.
        // The configured default is final-polish-only (memetic stays off); guarded, so it can only improve
        // the jDE best. Memetic in-loop NM tested with jDE (2026-06-04): it does NOT collapse — jDE stays in
        // the clean penalty-0 basin, so the memetic polish reached 0.15742 / penalty 0, basically the same
        // point as final-polish-only (0.15721) but a hair worse and ~138k generations earlier (it trims
        // diversity and accelerates stagnation for no gain). The earlier SHADE/L-SHADE memetic collapse
        // (0.921099 / penalty 0.5) was current-to-pbest already sitting in the penalized corner, not an
        // intrinsic flaw.
        var nelderMeadRefinement = configuration.NelderMead.ToRefinementSettings();

        var settingsBuilder = DifferentialEvolutionScenarioSettings
                        .CreateBuilder()
                        .WithMeter(meter)
                        .WithPropellantsFromFile(inputFileName)
                        .WithPopulationSize(populationSize)
                        .WithLowerBound(groupLowerBound)
                        .WithUpperBound(groupUpperBound)
                        .WithStrategy(strategy)
                        // F/CR below are only consumed by the Classic and jDE strategies; the adaptive variants self-tune them.
                        .WithMutationForce(deConfiguration.MutationForce)
                        .WithCrossoverProbability(deConfiguration.CrossoverProbability)
                        .WithTerminationStrategy(terminationStrategy)
                        .AddPenaltyEvaluators(penaltyEvaluators)
                        .WithProcessorsCount(processorsCount)
                        .WithNelderMeadRefinement(nelderMeadRefinement);

        if (maxEvaluationNumber.HasValue)
            settingsBuilder = settingsBuilder.WithMaxEvaluationNumber(maxEvaluationNumber.Value);

        if (pBestRate.HasValue && archiveSizeRate.HasValue && memorySize.HasValue)
            settingsBuilder = settingsBuilder.WithShadeParameters(pBestRate.Value, archiveSizeRate.Value, memorySize.Value);

        var settings = settingsBuilder.Build();

        // Provenance: record the values the run is ACTUALLY using — population and worker count after the
        // strategy branch and the divisibility-reduction rule, and the expanded 32-element bounds — not the
        // values the configuration file requested.
        var resolved = new ResolvedRunRecord
        {
            Mode = "optimization",
            ConfigurationSource = loadedConfiguration.SourceDescription,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            Configuration = configuration,
            Effective = new EffectiveRunValues
            {
                PopulationSize = populationSize,
                ProcessorsCount = processorsCount,
                MachineProcessorCount = Environment.ProcessorCount,
                Dimensions = dimensions,
                TerminationStrategy = terminationDescription,
                MaxEvaluationNumber = maxEvaluationNumber,
                PBestRate = pBestRate,
                ArchiveSizeRate = archiveSizeRate,
                MemorySize = memorySize,
                GroupLowerBound = groupLowerBound,
                GroupUpperBound = groupUpperBound
            }
        };

        return new GroupOptimizationPlan
        {
            Scenario = new GroupDifferentialEvolutionScenario(settings),
            Settings = settings,
            Meter = meter,
            PopulationSize = populationSize,
            ProcessorsCount = processorsCount,
            MaxEvaluationNumber = maxEvaluationNumber,
            PBestRate = pBestRate,
            ArchiveSizeRate = archiveSizeRate,
            MemorySize = memorySize,
            NelderMeadRefinement = nelderMeadRefinement,
            Resolved = resolved
        };
    }

    /// <summary>Runs the differential-evolution search, timing it on the plan's meter.</summary>
    public static async Task<OperationResult<GroupOptimizationResult>> RunAsync(GroupOptimizationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        using (plan.Meter.GetTotalExecutionTimeMeasurer().StartFrame())
            return await plan.Scenario.RunAsync();
    }

    /// <summary>
    /// Builds a scenario that solves one supplied vector rather than searching for one.
    ///
    /// <para>The configuration matters here even though no search runs: the penalty thresholds are part
    /// of the score, so a replay must be evaluated under the same configuration that produced the
    /// vector. The search-only settings (population, strategy, termination) are builder-required
    /// placeholders that no evaluation reads, which is why they are fixed rather than configured.</para>
    /// </summary>
    /// <param name="loadedConfiguration">The resolved run configuration and its provenance.</param>
    public static ForwardEvalPlan CreateForwardEvalPlan(LoadedRunConfiguration loadedConfiguration)
    {
        ArgumentNullException.ThrowIfNull(loadedConfiguration);

        var configuration = loadedConfiguration.Configuration;
        var groupLowerBound = configuration.Bounds.ToGroupLowerBound();
        var groupUpperBound = configuration.Bounds.ToGroupUpperBound();

        var meter = new PerformanceMeter();
        var settings = DifferentialEvolutionScenarioSettings.CreateBuilder()
            .WithMeter(meter)
            .WithPropellantsFromFile(configuration.InputFileName)
            .WithPopulationSize(groupLowerBound.Length) // unused (no DE run), but the builder requires a positive value
            .WithLowerBound(groupLowerBound)
            .WithUpperBound(groupUpperBound)
            .WithStrategy(DifferentialEvolutionStrategy.Jde)
            .WithMutationForce(0.5)
            .WithCrossoverProbability(0.9)
            .WithTerminationStrategy(new TimeoutTerminationStrategy(TimeSpan.FromMinutes(1)))
            .AddPenaltyEvaluators(GroupPenaltyEvaluatorFactory.Build(configuration.Penalties))
            .WithProcessorsCount(1)
            .Build();

        var resolved = new ResolvedRunRecord
        {
            Mode = "forward-eval",
            ConfigurationSource = loadedConfiguration.SourceDescription,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            Configuration = configuration,
            Effective = new EffectiveRunValues
            {
                // A forward eval is one single-threaded point evaluation; the recorded values are what it
                // genuinely used, so they must not be confused with the search sizing in the configuration.
                PopulationSize = 1,
                ProcessorsCount = 1,
                MachineProcessorCount = Environment.ProcessorCount,
                Dimensions = groupLowerBound.Length,
                TerminationStrategy = "not applicable (single point evaluation, no search)",
                GroupLowerBound = groupLowerBound,
                GroupUpperBound = groupUpperBound
            }
        };

        return new ForwardEvalPlan
        {
            Scenario = new GroupDifferentialEvolutionScenario(settings),
            Settings = settings,
            Meter = meter,
            Resolved = resolved
        };
    }

    /// <summary>Evaluates a single vector, timing it on the plan's meter.</summary>
    public static GroupOptimizationResult EvaluateVector(ForwardEvalPlan plan, double[] genes)
    {
        ArgumentNullException.ThrowIfNull(plan);

        using (plan.Meter.GetTotalExecutionTimeMeasurer().StartFrame())
            return plan.Scenario.EvaluateVector(genes);
    }
}
