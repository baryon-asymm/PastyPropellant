using System.Collections.ObjectModel;
using DifferentialEvolution.Optimizer;
using DifferentialEvolution.Optimizer.Interfaces;
using DifferentialEvolution.Optimizer.Models;
using DifferentialEvolution.Optimizer.MutationStrategies;
using DifferentialEvolution.Optimizer.PopulationGenerators;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Optimization.Optimizers;

public class DifferentialEvolutionOptimizerBuilder
{
    private int _maxStagnationStreak;
    private int _populationSize;
    private ReadOnlyCollection<double> _lowerBound;
    private ReadOnlyCollection<double> _upperBound;
    private IEnumerable<Propellant> _propellants;
    private IFitnessFunctionVisitor _fitnessFunctionEvaluator;
    private OptimizationProblemContextByDoubles[] _contextByDoubles;
    private OptimizationProblemContextByUnits _contextByUnits;

    public DifferentialEvolutionOptimizerBuilder(
        IEnumerable<Propellant> propellants)
    {
        _propellants = propellants;
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
        OptimizationProblemContextByDoubles[] contextByDoubles,
        OptimizationProblemContextByUnits contextByUnits)
    {
        _contextByDoubles = contextByDoubles;
        _contextByUnits = contextByUnits;
        return this;
    }

    public DifferentialEvolutionOptimizer Build()
    {
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

public class DifferentialEvolutionOptimizer : IParametricCombustionModelOptimizer, IFitnessFunctionInvoker
{
#region Fields

    private OptimizationProblemContextByDoubles[] _optimizationContexts;
    private OptimizationProblemContextByUnits _optimizationContextByUnits;

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
        OptimizationProblemContextByDoubles[] optimizationContexts,
        OptimizationProblemContextByUnits optimizationContextByUnits,
        int maxStagnationStreak,
        int populationSize,
        ReadOnlyCollection<double> lowerBound,
        ReadOnlyCollection<double> upperBound,
        IFitnessFunctionVisitor fitnessFunctionSolver)
    {
        _optimizationContexts = optimizationContexts ?? throw new ArgumentNullException(nameof(optimizationContexts));
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

    private Task<OperationResult<OptimizationResult>> GetOptimizationResultAsync()
    {
        try
        {
            var optimizationResult = TryGetOptimizationResult();
            return Task.FromResult(new OperationResult<OptimizationResult>(optimizationResult));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new OperationResult<OptimizationResult>(ex));
        }
    }

    private OptimizationResult TryGetOptimizationResult()
    {
        var populationGenerator = new PopulationGenerator(PopulationSize, UpperBound, LowerBound);
        var optimizer = new Optimizer(new MutationStrategy(LowerBound, UpperBound, 0.5, 0.9),
                                      this,
                                      populationGenerator.Generate(this),
                                      populationGenerator.Generate(this),
                                      MaxStagnationStreak,
                                      GetOutputCallbackInvoker());

        var individual = optimizer.Run();

        var solverParams = CombustionSolverParamsByUnits.FromVector(individual.Vector.Span);
        _optimizationContextByUnits.Accept(solverParams, FitnessFunctionSolver);

        var result = new OptimizationResult(individual.Vector, _optimizationContextByUnits);

        return result;
    }

    protected virtual Action<int, Individual> GetOutputCallbackInvoker()
    {
        Action<int, Individual> outputCallbackInvoker = (
            generation,
            individual) =>
        {
            Console.WriteLine($"Generation: {generation}, Best individual: {individual.FitnessFunctionCost}");
        };

        return outputCallbackInvoker;
    }

    public double Invoke(
        int threadIndex,
        Span<double> point)
    {
        var solverParams = CombustionSolverParamsByDoubles.FromVector(point);
        var context = _optimizationContexts[threadIndex];

        FitnessFunctionSolver.Visit(solverParams, context);

        if (context.FitnessFunctionValue == double.MaxValue)
            return double.MaxValue;

        return context.FitnessFunctionValue + context.TotalEvaluatedPenalty;
    }
}
