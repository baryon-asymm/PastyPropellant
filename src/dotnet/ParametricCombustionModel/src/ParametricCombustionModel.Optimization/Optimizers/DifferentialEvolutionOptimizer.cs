using System.Collections.ObjectModel;
using DotNetDifferentialEvolution;
using DotNetDifferentialEvolution.Interfaces;
using DotNetDifferentialEvolution.Models;
using DotNetDifferentialEvolution.TerminationStrategies;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Optimization.Optimizers;

public class DifferentialEvolutionOptimizerBuilder
{
    private int _maxStagnationStreak;
    private int _populationSize;
    private ReadOnlyCollection<double> _lowerBound;
    private ReadOnlyCollection<double> _upperBound;
    private IEnumerable<Propellant> _propellants;
    private IFitnessFunctionVisitor? _fitnessFunctionEvaluator;
    private OptimizationProblemByDoubles[] _contextByDoubles;
    private OptimizationProblemByUnits? _contextByUnits;

    public DifferentialEvolutionOptimizerBuilder(
        IEnumerable<Propellant> propellants)
    {
        _propellants = propellants;

        _lowerBound = ReadOnlyCollection<double>.Empty;
        _upperBound = ReadOnlyCollection<double>.Empty;
        _contextByDoubles = [];
    }

    public DifferentialEvolutionOptimizerBuilder WithMaxStagnationStreak(
        int maxStagnationStreak)
    {
        _maxStagnationStreak = maxStagnationStreak;
        return this;
    }

    public DifferentialEvolutionOptimizerBuilder WithPopulationSize(
        int populationSize)
    {
        _populationSize = populationSize;
        return this;
    }

    public DifferentialEvolutionOptimizerBuilder WithLowerBound(
        ReadOnlyCollection<double> lowerBound)
    {
        _lowerBound = lowerBound;
        return this;
    }

    public DifferentialEvolutionOptimizerBuilder WithUpperBound(
        ReadOnlyCollection<double> upperBound)
    {
        _upperBound = upperBound;
        return this;
    }

    public DifferentialEvolutionOptimizerBuilder WithFitnessFunctionEvaluator(
        IFitnessFunctionVisitor fitnessFunctionEvaluator)
    {
        _fitnessFunctionEvaluator = fitnessFunctionEvaluator;
        return this;
    }

    public DifferentialEvolutionOptimizerBuilder WithOptimizationContexts(
        OptimizationProblemByDoubles[] contextByDoubles,
        OptimizationProblemByUnits contextByUnits)
    {
        _contextByDoubles = contextByDoubles;
        _contextByUnits = contextByUnits;
        return this;
    }

    public DifferentialEvolutionOptimizer Build()
    {
        if (_contextByUnits is null)
            throw new InvalidOperationException("Optimization context by units is not set.");
        
        if (_fitnessFunctionEvaluator is null)
            throw new InvalidOperationException("Fitness function evaluator is not set.");

        return new DifferentialEvolutionOptimizer(
            _contextByDoubles,
            _contextByUnits,
            _maxStagnationStreak,
            _populationSize,
            _lowerBound,
            _upperBound,
            _fitnessFunctionEvaluator);
    }

    public static DifferentialEvolutionOptimizerBuilder FromPropellants(
        IEnumerable<Propellant> propellants)
    {
        return new DifferentialEvolutionOptimizerBuilder(propellants);
    }
}

public class DifferentialEvolutionOptimizer : IParametricCombustionModelOptimizer, IFitnessFunctionEvaluator
{
#region Fields

    private OptimizationProblemByDoubles[] _optimizationContexts;
    private OptimizationProblemByUnits _optimizationContextByUnits;

#endregion

#region Properties

    public int MaxStagnationStreak { get; init; }
    public int PopulationSize { get; init; }

    public ReadOnlyCollection<double> LowerBound { get; init; }
    public ReadOnlyCollection<double> UpperBound { get; init; }

    public IFitnessFunctionVisitor FitnessFunctionSolver { get; init; }

#endregion

#region Constructors

    public DifferentialEvolutionOptimizer(
        OptimizationProblemByDoubles[] optimizationContexts,
        OptimizationProblemByUnits optimizationContextByUnits,
        int maxStagnationStreak,
        int populationSize,
        ReadOnlyCollection<double> lowerBound,
        ReadOnlyCollection<double> upperBound,
        IFitnessFunctionVisitor fitnessFunctionSolver)
    {
        _optimizationContexts = optimizationContexts
            ?? throw new ArgumentNullException(nameof(optimizationContexts));
        _optimizationContextByUnits = optimizationContextByUnits
            ?? throw new ArgumentNullException(nameof(optimizationContextByUnits));
        MaxStagnationStreak = maxStagnationStreak;
        PopulationSize = populationSize;
        LowerBound = lowerBound ?? throw new ArgumentNullException(nameof(lowerBound));
        UpperBound = upperBound ?? throw new ArgumentNullException(nameof(upperBound));
        FitnessFunctionSolver = fitnessFunctionSolver ?? throw new ArgumentNullException(nameof(fitnessFunctionSolver));
    }

#endregion

    public Task<OperationResult<OptimizationResult>> RunAsync()
    {
        return GetOptimizationResultAsync();
    }

    private async Task<OperationResult<OptimizationResult>> GetOptimizationResultAsync()
    {
        try
        {
            var optimizationResult = await TryGetOptimizationResultAsync();
            return new OperationResult<OptimizationResult>(optimizationResult);
        }
        catch (Exception ex)
        {
            return new OperationResult<OptimizationResult>(ex);
        }
    }

    private async Task<OptimizationResult> TryGetOptimizationResultAsync()
    {
        var terminationStrategy =
            new StagnationStreakTerminationStrategy(maxStagnationStreak: MaxStagnationStreak, stagnationThreshold: 1e-6);
        using var de = DifferentialEvolutionBuilder.ForFunction(this)
                                          .WithBounds(LowerBound.ToArray(), UpperBound.ToArray())
                                          .WithPopulationSize(PopulationSize)
                                          .WithUniformPopulationSampling()
                                          .WithDefaultMutationStrategy(
                                              mutationForce: 0.5, crossoverProbability: 0.9)
                                          .WithDefaultSelectionStrategy()
                                          .WithTerminationCondition(terminationStrategy)
                                          .UseProcessors(processorsCount: 8)
                                          .WithPopulationUpdateHandler(new PopulationUpdateHandler())
                                          .Build();

        var population = await de.RunAsync();

        _optimizationContextByUnits.Accept(
            CombustionSolverParamsByUnits.FromVector(population.IndividualCursor.Genes.ToArray()),
            FitnessFunctionSolver);

        var result = new OptimizationResult(
            lowerBound: LowerBound.ToArray(),
            upperBound: UpperBound.ToArray(),
            bestParams: population.IndividualCursor.Genes.ToArray(),
            _optimizationContextByUnits);

        return result;
    }
    
    private class PopulationUpdateHandler : IPopulationUpdatedHandler
    {
        private DateTime _lastUpdate = DateTime.Now;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(3);
        
        public void Handle(
            Population population)
        {
            if (DateTime.Now - _lastUpdate < _updateInterval)
                return;

            population.MoveCursorToBestIndividual();
            
            _lastUpdate = DateTime.Now;

            EventBus<InfoLogEvent>.Publish(
                new InfoLogEvent(
                    $"Generation: {population.GenerationNumber}, Best individual: {population.IndividualCursor.FitnessFunctionValue}",
                    nameof(DifferentialEvolutionOptimizer)
                )
            );
        }
    }

    public double Evaluate(
        ReadOnlySpan<double> genes) =>
        Evaluate(workerIndex: 0, genes);

    public double Evaluate(
        int workerIndex,
        ReadOnlySpan<double> genes)
    {
        var solverParams = CombustionSolverParamsByDoubles.FromVector(genes);
        var context = _optimizationContexts[workerIndex];

        FitnessFunctionSolver.Visit(solverParams, context);

        if (context.FitnessFunctionValue == double.MaxValue)
            return double.MaxValue;

        return context.FitnessFunctionValue + context.TotalEvaluatedPenalty;
    }
}
