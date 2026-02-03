using DotNetDifferentialEvolution;
using DotNetDifferentialEvolution.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.Optimization.Settings;
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
    /// groupIndex: 0=bas_0, 1=bas_1, 2=bas_2+bas_3+bas_4
    /// </summary>
    private readonly OptimizationProblemByDoubles[,] _compositionProblems;
    
    /// <summary>
    /// Array of contexts for final result evaluation with UnitsNet types
    /// Index: 0=bas_0, 1=bas_1, 2=bas_2+bas_3+bas_4
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
    /// <param name="compositionProblems">Matrix of problems [workerIndex, groupIndex] where groupIndex: 0=bas_0, 1=bas_1, 2=bas_2</param>
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

        var builder = DifferentialEvolutionBuilder.ForFunction(this)
                                          .WithBounds(lowerBound, upperBound)
                                          .WithPopulationSize(_settings.PopulationSize)
                                          .WithUniformPopulationSampling()
                                          .WithDefaultMutationStrategy(
                                              mutationForce: _settings.MutationForce,
                                              crossoverProbability: _settings.CrossoverProbability)
                                          .WithDefaultSelectionStrategy()
                                          .WithTerminationCondition(_settings.TerminationStrategy)
                                          .UseProcessors(processorsCount: _settings.ProcessorsCount);

        if (_settings.PopulationUpdatedHandler != null)
            builder = builder.WithPopulationUpdateHandler(_settings.PopulationUpdatedHandler);

        using var de = builder.Build();

        var population = await de.RunAsync();

        // Evaluate final best solution using UnitsNet types for each composition group
        var bestGroupParams = GroupCombustionSolverParamsByUnits.FromVector(population.IndividualCursor.Genes.ToArray());
        
        for (var groupIdx = 0; groupIdx < 3; groupIdx++)
        {
            var compositionParams = bestGroupParams.ToCompositionVector(groupIdx);
            var combustionParams = CombustionSolverParamsByUnits.FromVector(compositionParams);
            _fitnessFunctionEvaluator.Visit(combustionParams, _finalContextsByUnits[groupIdx]);
        }

        var result = new GroupOptimizationResult(
            compositionContexts: _finalContextsByUnits,
            lowerBound: lowerBound,
            upperBound: upperBound,
            bestParams: population.IndividualCursor.Genes.ToArray());

        return result;
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

