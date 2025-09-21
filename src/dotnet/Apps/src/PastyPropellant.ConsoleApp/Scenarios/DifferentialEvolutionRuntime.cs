using System.Collections.ObjectModel;
using System.Text.Json;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Optimizers;
using PastyPropellant.Core.Utils;
using UnitsNet;

namespace ParametricCombustionModel.ProcessWorker.Scenarios;

public class DifferentialEvolutionRuntime
{
    private readonly ReadOnlyCollection<double> _lowerBound;
    private readonly ReadOnlyCollection<double> _upperBound;
    
    private readonly DifferentialEvolutionOptimizer _optimizer;

    public DifferentialEvolutionRuntime(
        int populationSize,
        int maxStagnationStreak,
        string propellantsFilePath,
        IEnumerable<double> lowerBound,
        IEnumerable<double> upperBound,
        double poreDiameterThreshold = 3.0)
    {
        _lowerBound = lowerBound.ToList().AsReadOnly();
        _upperBound = upperBound.ToList().AsReadOnly();

        if (_lowerBound.Count != _upperBound.Count)
            throw new ArgumentException("Lower and upper bounds must have the same length");

        var propellants = GetPropellants(propellantsFilePath);
        var penaltyEvaluators = GetPenaltyEvaluators(poreDiameterThreshold: poreDiameterThreshold);
        var optimizationProblemContext = GetOptimizationProblemContextByDoubles(
            propellants,
            penaltyEvaluators);
        var contextMatrixByUnits = GetProblemContextMatrixByUnits(propellants);
        var optimizationProblemContextByUnits = GetOptimizationProblemContextByUnits(
            contextMatrixByUnits,
            penaltyEvaluators);
        var fitnessFunctionEvaluator = GetTargetFunctionSolver();

        _optimizer = DifferentialEvolutionOptimizerBuilder.FromPropellants(propellants)
                                                          .WithLowerBound(lowerBound.ToArray().AsReadOnly())
                                                          .WithUpperBound(upperBound.ToArray().AsReadOnly())
                                                          .WithOptimizationContexts(
                                                              optimizationProblemContext,
                                                              optimizationProblemContextByUnits)
                                                          .WithPopulationSize(populationSize)
                                                          .WithFitnessFunctionEvaluator(fitnessFunctionEvaluator)
                                                          .WithMaxStagnationStreak(maxStagnationStreak)
                                                          .Build();
    }

    private ReadOnlyCollection<Propellant> GetPropellants(
        string filePath)
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = JsonSerializer.DeserializeAsync<List<Propellant>>(fileStream).Result
            ?? throw new InvalidOperationException("Failed to deserialize propellants from file.");
        return result.AsReadOnly();
    }

    private ProblemContextByDoubles[,] GetProblemContextMatrixByDoubles(
        IEnumerable<Propellant> propellants)
    {
        var contextMatrix = ProblemContextByDoublesMatrixBuilder.FromPropellants(propellants)
                                                                .BuildMatrix();

        return contextMatrix;
    }

    private ProblemContextByUnits[,] GetProblemContextMatrixByUnits(
        IEnumerable<Propellant> propellants)
    {
        var contextMatrix = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants)
                                                              .BuildMatrix();

        return contextMatrix;
    }

    private IEnumerable<IPenaltyEvaluator> GetPenaltyEvaluators(double poreDiameterThreshold = 3.0)
    {
        var penaltyRate = 1.0;
        var heatFluxRatioThreshold = 100.0;

        var maxInterPocketKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e9);
        var maxSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);
        var maxOutSkeletonKineticFlameHeatFlux = HeatFlux.FromWattsPerSquareMeter(1e8);

        var penaltyEvaluators = new List<IPenaltyEvaluator>
        {
            new PocketHeatFluxRatioCompetitionPenaltyEvaluator(penaltyRate, heatFluxRatioThreshold),
            new InterPocketFasterBurnPenaltyEvaluator(penaltyRate),
            new KineticFlameHeatFluxPenaltyEvaluator(penaltyRate,
                                                     maxInterPocketKineticFlameHeatFlux,
                                                     maxSkeletonKineticFlameHeatFlux,
                                                     maxOutSkeletonKineticFlameHeatFlux),
            new PoreDiameterPenaltyEvaluator(penaltyRate, poreDiameterThreshold),
            // new RadiativeThermalConductivityPenaltyEvaluator(penaltyRate)
        };

        return penaltyEvaluators;
    }

    private OptimizationProblemByDoubles[] GetOptimizationProblemContextByDoubles(
        IEnumerable<Propellant> propellants,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators)
    {
        var solver = new MixedPropellantSolver();
        var contexts = new List<OptimizationProblemByDoubles>();
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            var contextMatrix = GetProblemContextMatrixByDoubles(propellants);
            contexts.Add(new OptimizationProblemByDoubles(contextMatrix, solver, penaltyEvaluators));
        }

        return contexts.ToArray();
    }

    private OptimizationProblemByUnits GetOptimizationProblemContextByUnits(
        ProblemContextByUnits[,] contextMatrix,
        IEnumerable<IPenaltyEvaluator> penaltyEvaluators)
    {
        var solver = new MixedPropellantSolver();
        var context = new OptimizationProblemByUnits(contextMatrix, solver, penaltyEvaluators);
        return context;
    }

    private IFitnessFunctionVisitor GetTargetFunctionSolver()
    {
        var solver = new PenaltyFitnessFunctionEvaluator();
        return solver;
    }

    public Task<OperationResult<OptimizationResult>> RunAsync()
    {
        return _optimizer.RunAsync();
    }
}
