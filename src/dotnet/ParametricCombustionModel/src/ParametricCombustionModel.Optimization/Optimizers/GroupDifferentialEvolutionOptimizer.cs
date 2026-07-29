using DotNetDifferentialEvolution;
using DotNetNelderMead;
using DotNetNelderMead.DifferentialEvolution;
using DotNetOptimization.Abstractions;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Optimization.Optimizers;

/// <summary>
/// Differential Evolution optimizer for group optimization of three propellant compositions.
/// Manages 32-parameter unified optimization vector and distributes evaluation across three composition groups.
/// Uses standard PenaltyFitnessFunctionEvaluator for each group independently.
/// </summary>
public class GroupDifferentialEvolutionOptimizer : IFitnessFunctionEvaluator
{
#region Fields

    private readonly DifferentialEvolutionSettings _settings;
    
    /// <summary>
    /// Matrix of optimization problems: [workerIndex, groupIndex]
    /// groupIndex follows the raw 32-vector layout: 0=Bas_2+Bas_3+Bas_4, 1=Bas_1, 2=Bas_0
    /// </summary>
    private readonly OptimizationProblemByDoubles[,] _compositionProblems;

    /// <summary>
    /// Array of contexts for final result evaluation with UnitsNet types.
    /// Index follows the raw 32-vector layout: 0=Bas_2+Bas_3+Bas_4, 1=Bas_1, 2=Bas_0
    /// </summary>
    private readonly OptimizationProblemByUnits[] _finalContextsByUnits;
    
    /// <summary>
    /// Fitness function evaluator with penalty support
    /// </summary>
    private readonly PenaltyFitnessFunctionEvaluator _fitnessFunctionEvaluator;

#endregion

#region Properties

    public IFitnessFunctionVisitor FitnessFunctionSolver { get; init; }

#endregion

#region Constructors

    /// <summary>
    /// Initializes GroupDifferentialEvolutionOptimizer for simultaneous optimization of three propellant compositions.
    /// </summary>
    /// <param name="settings">Optimization settings including bounds, population size, etc.</param>
    /// <param name="compositionProblems">Matrix of problems [workerIndex, groupIndex] where groupIndex follows the raw 32-vector layout: 0=Bas_2+Bas_3+Bas_4, 1=Bas_1, 2=Bas_0</param>
    /// <param name="finalContextsByUnits">Array of contexts for result evaluation, one per composition group</param>
    public GroupDifferentialEvolutionOptimizer(
        DifferentialEvolutionSettings settings,
        OptimizationProblemByDoubles[,] compositionProblems,
        OptimizationProblemByUnits[] finalContextsByUnits)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _compositionProblems = compositionProblems ?? throw new ArgumentNullException(nameof(compositionProblems));
        _finalContextsByUnits = finalContextsByUnits ?? throw new ArgumentNullException(nameof(finalContextsByUnits));
        
        if (compositionProblems.GetLength(1) != 3)
            throw new ArgumentException("Must have exactly 3 composition groups", nameof(compositionProblems));
        
        if (finalContextsByUnits.Length != 3)
            throw new ArgumentException("Must have exactly 3 final contexts", nameof(finalContextsByUnits));

        _fitnessFunctionEvaluator = new PenaltyFitnessFunctionEvaluator();
        FitnessFunctionSolver = _fitnessFunctionEvaluator;
    }

#endregion

    public Task<OperationResult<GroupOptimizationResult>> RunAsync()
    {
        return GetOptimizationResultAsync();
    }

    private async Task<OperationResult<GroupOptimizationResult>> GetOptimizationResultAsync()
    {
        try
        {
            var optimizationResult = await TryGetOptimizationResultAsync();
            return new OperationResult<GroupOptimizationResult>(optimizationResult);
        }
        catch (Exception ex)
        {
            return new OperationResult<GroupOptimizationResult>(ex);
        }
    }

    private async Task<GroupOptimizationResult> TryGetOptimizationResultAsync()
    {
        var lowerBound = _settings.LowerBound.ToArray();
        var upperBound = _settings.UpperBound.ToArray();
        var nelderMead = _settings.NelderMead;

        var builder = DifferentialEvolutionBuilder.ForFunction(this)
                                          .WithBounds(lowerBound, upperBound)
                                          .WithPopulationSize(_settings.PopulationSize)
                                          .WithUniformPopulationSampling()
                                          .ApplyStrategy(_settings)
                                          .WithTerminationCondition(_settings.TerminationStrategy)
                                          .UseProcessors(processorsCount: _settings.ProcessorsCount);

        if (_settings.PopulationUpdatedHandler != null)
            builder = builder.WithPopulationUpdateHandler(_settings.PopulationUpdatedHandler);

        // A seeded run is reproducible at THIS worker count and no other: the library derives one stream
        // per worker and individual i draws from worker (i mod workerCount)'s stream. Left unseeded the
        // library seeds itself, which is what every run before the 5.x upgrade did.
        if (_settings.Seed.HasValue)
            builder = builder.WithSeed(_settings.Seed.Value);

        // Memetic stage: an in-loop Nelder–Mead refiner polishes the best individual every N generations.
        // Bounds and the start point are injected by the refiner from the DE problem context; we only
        // tune simplex coefficients/convergence/restarts. Evaluations route through the same worker-indexed
        // Evaluate(...) overload, so they reuse the per-worker composition contexts.
        if (nelderMead is { Enabled: true, MemeticInLoop: true })
            builder = builder.WithNelderMeadLocalSearch(
                this,
                nelderMead.EveryNGenerations,
                nelderMead.MemeticMaxEvaluationsPerCall,
                ApplyNelderMeadTuning);

        using var de = builder.Build();

        var population = await de.RunAsync();
        population.MoveCursorToBestIndividual();

        var bestGenes = population.IndividualCursor.Genes.ToArray();
        var bestFitness = population.IndividualCursor.FitnessFunctionValue;

        // Final stage: a sequential DE→Nelder–Mead handoff polishes the converged best once, seeded from
        // the DE solution. Single simplex (worker 0), so it reuses the [0, *] composition contexts.
        if (nelderMead is { Enabled: true, FinalPolish: true })
            (bestGenes, bestFitness) = ApplyFinalPolish(bestGenes, bestFitness, lowerBound, upperBound);

        // Final evaluation reuses the single-vector forward pass exposed for point evaluation,
        // so the converged-best report and forward-eval share exactly one code path.
        return EvaluateVector(bestGenes);
    }

    /// <summary>
    /// Forward-evaluates a single group parameter vector with no DE search: applies it to each
    /// composition's UnitsNet context and builds the fully populated <see cref="GroupOptimizationResult"/>.
    /// This is the same final-evaluation pass the optimizer runs after convergence, exposed so a caller
    /// can compute and report any point in parameter space directly (seconds instead of a full run).
    /// </summary>
    public GroupOptimizationResult EvaluateVector(double[] genes)
    {
        var groupParams = GroupCombustionSolverParamsByUnits.FromVector(genes);

        for (var groupIdx = 0; groupIdx < 3; groupIdx++)
        {
            var compositionParams = groupParams.ToCompositionVector(groupIdx);
            var combustionParams = CombustionSolverParamsByUnits.FromVector(compositionParams);
            _fitnessFunctionEvaluator.Visit(combustionParams, _finalContextsByUnits[groupIdx]);
        }

        return new GroupOptimizationResult(
            compositionContexts: _finalContextsByUnits,
            lowerBound: _settings.LowerBound.ToArray(),
            upperBound: _settings.UpperBound.ToArray(),
            bestParams: genes);
    }

    /// <summary>
    /// Runs the final sequential Nelder–Mead polish from the DE best and returns the better of the two
    /// solutions. The simplex can drift across penalty cliffs, so the DE solution is kept unless the
    /// refined fitness is at least as good (best-of-both guard).
    /// </summary>
    private (double[] genes, double fitness) ApplyFinalPolish(
        double[] deBestGenes,
        double deBestFitness,
        double[] lowerBound,
        double[] upperBound)
    {
        var refined = ApplyNelderMeadTuning(
                NelderMeadBuilder.ForFunction(this)
                    .StartingFrom(deBestGenes)
                    .WithBounds(lowerBound, upperBound))
            .WithMaxEvaluations(_settings.NelderMead.FinalPolishMaxEvaluations)
            .Build()
            .Run();

        if (refined.Best.FitnessFunctionValue <= deBestFitness)
        {
            EventBus<InfoLogEvent>.Publish(new InfoLogEvent(
                $"Final Nelder–Mead polish improved best: {deBestFitness:G6} -> {refined.Best.FitnessFunctionValue:G6} " +
                $"({refined.EvaluationCount} evals, {refined.TerminationReason}).",
                nameof(GroupDifferentialEvolutionOptimizer)));

            return (refined.Best.Genes.ToArray(), refined.Best.FitnessFunctionValue);
        }

        EventBus<InfoLogEvent>.Publish(new InfoLogEvent(
            $"Final Nelder–Mead polish did not improve DE best ({deBestFitness:G6}); keeping DE solution.",
            nameof(GroupDifferentialEvolutionOptimizer)));

        return (deBestGenes, deBestFitness);
    }

    /// <summary>Applies the shared Nelder–Mead tuning (coefficients, convergence, restarts) from settings.</summary>
    private NelderMeadBuilder ApplyNelderMeadTuning(NelderMeadBuilder builder)
    {
        var nelderMead = _settings.NelderMead;

        builder = nelderMead.AdaptiveCoefficients
            ? builder.WithAdaptiveCoefficients()
            : builder.WithStandardCoefficients();

        builder = builder.WithConvergence(nelderMead.DomainTolerance, nelderMead.FunctionTolerance);

        if (nelderMead.Restarts > 0)
            builder = builder.WithRestarts(nelderMead.Restarts);

        return builder;
    }

    /// <summary>
    /// Evaluates fitness for a 32-parameter group vector using default worker (0).
    /// </summary>
    public double Evaluate(ReadOnlySpan<double> genes) =>
        Evaluate(workerIndex: 0, genes);

    /// <summary>
    /// Evaluates fitness for a 32-parameter group vector for specified worker.
    /// Converts group vector to three 18-parameter vectors, evaluates each composition independently,
    /// and returns aggregated fitness with penalties.
    /// </summary>
    public double Evaluate(
        int workerIndex,
        ReadOnlySpan<double> genes)
    {
        var groupSolverParams = GroupCombustionSolverParamsByDoubles.FromVector(genes);
        
        double totalFitness = 0.0;
        double totalPenalty = 0.0;
        
        // Evaluate each composition group independently
        for (var groupIdx = 0; groupIdx < 3; groupIdx++)
        {
            // Convert group 32-param vector to composition 18-param vector
            var compositionParams = groupSolverParams.ToCompositionVector(groupIdx);
            var combustionParams = CombustionSolverParamsByDoubles.FromVector(compositionParams);
            
            // Evaluate fitness for this composition group
            var context = _compositionProblems[workerIndex, groupIdx];
            context.Accept(combustionParams, _fitnessFunctionEvaluator);
            
            // Accumulate fitness and penalties
            if (context.FitnessFunctionValue == double.MaxValue)
                return double.MaxValue;
            
            totalFitness += context.FitnessFunctionValue;
            totalPenalty += context.TotalEvaluatedPenalty;
        }
        
        // Return average fitness plus total penalties
        return (totalFitness / 3.0) + totalPenalty;
    }
}

