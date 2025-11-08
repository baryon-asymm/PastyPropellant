using DotNetDifferentialEvolution;
using DotNetDifferentialEvolution.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Settings;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Optimization.Optimizers;

public class DifferentialEvolutionOptimizer : IParametricCombustionModelOptimizer, IFitnessFunctionEvaluator
{
#region Fields

    private readonly DifferentialEvolutionSettings _settings;

    private readonly OptimizationProblemByDoubles[] _optimizationContexts;
    private readonly OptimizationProblemByUnits _optimizationContextByUnits;

#endregion

#region Properties

    public IFitnessFunctionVisitor FitnessFunctionSolver { get; init; }

#endregion

#region Constructors

    public DifferentialEvolutionOptimizer(
        DifferentialEvolutionSettings settings,
        OptimizationProblemByDoubles[] optimizationContexts,
        OptimizationProblemByUnits optimizationContextByUnits,
        IFitnessFunctionVisitor fitnessFunctionSolver)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _optimizationContexts = optimizationContexts;
        _optimizationContextByUnits = optimizationContextByUnits;
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

        _optimizationContextByUnits.Accept(
            CombustionSolverParamsByUnits.FromVector(population.IndividualCursor.Genes.ToArray()),
            FitnessFunctionSolver);

        var result = new OptimizationResult(
            lowerBound: lowerBound,
            upperBound: upperBound,
            bestParams: population.IndividualCursor.Genes.ToArray(),
            context: _optimizationContextByUnits);

        return result;
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

        context.Accept(solverParams, FitnessFunctionSolver);
        // FitnessFunctionSolver.Visit(solverParams, context);

        if (context.FitnessFunctionValue == double.MaxValue)
            return double.MaxValue;

        return context.FitnessFunctionValue + context.TotalEvaluatedPenalty;
    }
}
