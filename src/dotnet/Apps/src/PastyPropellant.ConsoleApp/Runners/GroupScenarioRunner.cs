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
    /// Builds the production optimisation run: bounds, penalties, strategy, population, termination and
    /// Nelder–Mead refinement, plus the scenario itself.
    /// </summary>
    /// <param name="inputFileName">Propellants file to optimise against.</param>
    public static GroupOptimizationPlan CreateOptimizationPlan(string inputFileName)
    {
        var meter = new PerformanceMeter();

        var penaltyEvaluators = GroupPenaltyEvaluatorFactory.Build();

        var groupLowerBound = BoundsProvider.GetGroupLowerBound();
        var groupUpperBound = BoundsProvider.GetGroupUpperBound();
        var dimensions = groupLowerBound.Length; // 32

        // jDE (self-adaptive rand/1) is the winning strategy on this branch: its exploration reaches the
        // clean basin (obj 0.157 / penalty 0), whereas current-to-pbest variants either trap in a penalized
        // degenerate corner (SHADE/L-SHADE → 0.92) or converge prematurely to a worse optimum (JADE → 0.42).
        var strategy = DifferentialEvolutionStrategy.Jde;
        var maxAvailableProcessors = Math.Max(1, Environment.ProcessorCount - 1);
        var safetyTimeout = new TimeoutTerminationStrategy(TimeSpan.FromHours(9));

        int populationSize;
        int processorsCount;
        OrTerminationStrategy terminationStrategy;
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
            populationSize = dimensions * 18; // 32 * 18 = 576
            processorsCount = maxAvailableProcessors; // population shrinks, so the divisibility heuristic does not apply
            maxEvaluationNumber = 5_000_000;
            // Canonical L-SHADE control parameters (Tanabe & Fukunaga 2014): p-best 0.11, archive rate 2.6, memory 6.
            pBestRate = 0.11;
            archiveSizeRate = 2.6;
            memorySize = 6;
            terminationStrategy = new OrTerminationStrategy(
                new LimitEvaluationNumberTerminationStrategy(maxEvaluationNumber.Value),
                safetyTimeout);
        }
        else
        {
            // Fixed-population variants (Classic / jDE / JADE / SHADE): keep the stagnation+timeout
            // run control and pick a worker count that divides the population evenly for balanced load.
            populationSize = dimensions * 12; // 32 * 12 = 384 (jDE production population — reaches obj 0.157 / penalty 0)
            processorsCount = maxAvailableProcessors;
            for (; processorsCount >= 14; processorsCount--)
                if (populationSize % processorsCount == 0)
                    break;
            terminationStrategy = new OrTerminationStrategy(
                new CustomStagnationStreakTerminationStrategy(
                    maxStagnationStreak: 5_000,
                    relativeStagnationThreshold: 1e-6),
                safetyTimeout);
        }

        // Nelder–Mead refinement layered on top of the DE search (DotNetNelderMead.DifferentialEvolution):
        // an in-loop memetic refiner every N generations plus a final sequential polish of the converged best.
        // Surfaced here like `strategy`; set Enabled = false to fall back to plain DE.
        var nelderMeadRefinement = new NelderMeadRefinementSettings
        {
            Enabled = true, // final-polish-only (memetic stays off); guarded, so it can only improve the jDE best
            // Memetic in-loop NM is OFF. Tested with jDE (2026-06-04): it does NOT collapse — jDE stays in the
            // clean penalty-0 basin, so the memetic polish reached 0.15742 / penalty 0, basically the same point
            // as final-polish-only (0.15721) but a hair worse and ~138k generations earlier (it trims diversity
            // and accelerates stagnation for no gain). The earlier SHADE/L-SHADE memetic collapse (0.921099 /
            // penalty 0.5) was current-to-pbest already sitting in the penalized corner, not an intrinsic flaw.
            MemeticInLoop = false,
            EveryNGenerations = 50,
            MemeticMaxEvaluationsPerCall = 200,
            // Final polish runs once after L-SHADE converges (outside the budget) — give it room.
            FinalPolish = true,
            FinalPolishMaxEvaluations = 100_000,
            AdaptiveCoefficients = true,
            DomainTolerance = 1e-8,
            FunctionTolerance = 1e-8,
            Restarts = 2,
        };

        var settingsBuilder = DifferentialEvolutionScenarioSettings
                        .CreateBuilder()
                        .WithMeter(meter)
                        .WithPropellantsFromFile(inputFileName)
                        .WithPopulationSize(populationSize)
                        .WithLowerBound(groupLowerBound)
                        .WithUpperBound(groupUpperBound)
                        .WithStrategy(strategy)
                        // F/CR below are only consumed by the Classic and jDE strategies; the adaptive variants self-tune them.
                        .WithMutationForce(0.5)
                        .WithCrossoverProbability(0.9)
                        .WithTerminationStrategy(terminationStrategy)
                        .AddPenaltyEvaluators(penaltyEvaluators)
                        .WithProcessorsCount(processorsCount)
                        .WithNelderMeadRefinement(nelderMeadRefinement);

        if (maxEvaluationNumber.HasValue)
            settingsBuilder = settingsBuilder.WithMaxEvaluationNumber(maxEvaluationNumber.Value);

        if (pBestRate.HasValue && archiveSizeRate.HasValue && memorySize.HasValue)
            settingsBuilder = settingsBuilder.WithShadeParameters(pBestRate.Value, archiveSizeRate.Value, memorySize.Value);

        var settings = settingsBuilder.Build();

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
            NelderMeadRefinement = nelderMeadRefinement
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
    /// </summary>
    /// <param name="inputFileName">Propellants file to evaluate against.</param>
    public static ForwardEvalPlan CreateForwardEvalPlan(string inputFileName)
    {
        var groupLowerBound = BoundsProvider.GetGroupLowerBound();

        var meter = new PerformanceMeter();
        var settings = DifferentialEvolutionScenarioSettings.CreateBuilder()
            .WithMeter(meter)
            .WithPropellantsFromFile(inputFileName)
            .WithPopulationSize(groupLowerBound.Length) // unused (no DE run), but the builder requires a positive value
            .WithLowerBound(groupLowerBound)
            .WithUpperBound(BoundsProvider.GetGroupUpperBound())
            .WithStrategy(DifferentialEvolutionStrategy.Jde)
            .WithMutationForce(0.5)
            .WithCrossoverProbability(0.9)
            .WithTerminationStrategy(new TimeoutTerminationStrategy(TimeSpan.FromMinutes(1)))
            .AddPenaltyEvaluators(GroupPenaltyEvaluatorFactory.Build())
            .WithProcessorsCount(1)
            .Build();

        return new ForwardEvalPlan
        {
            Scenario = new GroupDifferentialEvolutionScenario(settings),
            Settings = settings,
            Meter = meter
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
