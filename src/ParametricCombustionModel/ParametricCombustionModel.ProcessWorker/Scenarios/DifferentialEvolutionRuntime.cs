using System.Collections.ObjectModel;
using System.Text.Json;
using DifferentialEvolution.Optimizer;
using DifferentialEvolution.Optimizer.Interfaces;
using DifferentialEvolution.Optimizer.Models;
using DifferentialEvolution.Optimizer.MutationStrategies;
using DifferentialEvolution.Optimizer.PopulationGenerators.Interfaces;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.DTOs;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.Constrainers;
using ParametricCombustionModel.Optimization.TargetFunctions;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Builders;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.ProcessWorker.Scenarios;

public class DifferentialEvolutionRuntime
{
    private readonly FitnessFunctionInvoker _fitnessFunctionInvoker;

    private readonly ReadOnlyCollection<double> _lowerBound;
    private readonly DifferentialEvolutionOptimizer _optimizer;

    private readonly ITargetFunctionSolver _solver;
    private readonly ReadOnlyCollection<double> _upperBound;

    public DifferentialEvolutionRuntime(IEnumerable<double> lowerBound,
                                        IEnumerable<double> upperBound,
                                        IEnumerable<double> pressures,
                                        double heatFlowsSegmentSize,
                                        int numberOfGenerations,
                                        string propellantsFileName,
                                        IPopulationGenerator firstPopulationGenerator,
                                        IPopulationGenerator secondPopulationGenerator)
    {
        _lowerBound = lowerBound.ToList().AsReadOnly();
        _upperBound = upperBound.ToList().AsReadOnly();

        if (_lowerBound.Count != _upperBound.Count)
            throw new ArgumentException("Lower and upper bounds must have the same length");

        var context = GetOptimizationContext($"../data/{propellantsFileName}", pressures);
        var solverMatrix = GetMixedPropellantSolvers(context);
        _solver = GetTargetFunctionSolver(context, solverMatrix);
        double[] penaltyRates = [1e1, 1e1, 1, 1];
        //var surfaceTemperaturesConstrainer = new SurfaceTemperaturesConstrainer(context.MinSurfaceTemperature, context.MaxSurfaceTemperature, penaltyRates[0], solverMatrix, _solver);
        var heatFlowsConstrainer =
            new PocketHeatFlowOffsetConstrainer(heatFlowsSegmentSize, penaltyRates[1], solverMatrix, _solver);
        var interPocketConstrainer =
            new InterPocketBurningFasterConstrainer(penaltyRates[2], solverMatrix, _solver, heatFlowsConstrainer);
        //var kineticFlameHeightConstrainer = new KineticFlameHeightConstrainer(100e-6, 1000e-6, penaltyRates[3], solverMatrix, _solver, interPocketConstrainer);
        var kineticFlameHeatFlowConstrainer =
            new KineticFlameHeatFlowConstrainer(1e8, penaltyRates[3], solverMatrix, _solver, interPocketConstrainer);

        _fitnessFunctionInvoker = new FitnessFunctionInvoker(_solver, kineticFlameHeatFlowConstrainer);

        _optimizer = new DifferentialEvolutionOptimizer(new MutationStrategy(lowerBound, upperBound, 0.5, 0.9),
                                                        _fitnessFunctionInvoker,
                                                        firstPopulationGenerator.Generate(_fitnessFunctionInvoker),
                                                        secondPopulationGenerator.Generate(_fitnessFunctionInvoker),
                                                        numberOfGenerations,
                                                        Callback);
    }

    private void Callback(int generation, Individual best)
    {
        if (generation % 1000 != 0)
            return;
        
        var penaltyValue = _fitnessFunctionInvoker.GetTotalPenalty(best.Vector.Span);
        var targetValue = best.FitnessFunctionCost - penaltyValue;
        EventBus<string>
            .Publish(
                $"Generation: {generation}\tBestFVal {best.FitnessFunctionCost:#,0.000000000}\tTargetValue {targetValue:#,0.000000000}\t\tPenalty {penaltyValue:#,0.000000000}");
    }

    private OptimizationContext GetOptimizationContext(string propellantsFilePath, IEnumerable<double> pressures)
    {
        var propellants = GetPropellants(propellantsFilePath);
        return new OptimizationContext(pressures, null, 0, 0, _lowerBound, _upperBound, 600, 750, propellants);
    }

    private ReadOnlyCollection<Propellant> GetPropellants(string filePath)
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = JsonSerializer.DeserializeAsync<List<Propellant>>(fileStream).Result;
        return result.AsReadOnly();
    }

    private ReadOnlyCollection<ReadOnlyCollection<MixedSolverParamsRecorder>> GetMixedPropellantSolvers(
        OptimizationContext context)
    {
        var solvers = MixedSolverParamsRecordersBuilder.FromPropellants(context.Propellants)
                                                       .ForPressures(context.Pressures)
                                                       .Build();
        return solvers.Select(x => x.ToList().AsReadOnly()).ToList().AsReadOnly();
    }

    private ITargetFunctionSolver GetTargetFunctionSolver(OptimizationContext context,
                                                          IEnumerable<IEnumerable<MixedPropellantSolver>> solverMatrix)
    {
        var experimentalBurningRates = context.Propellants.GetExperimentalBurningRates(context.Pressures);
        var solver = new TargetFunctionNonlconSolver(experimentalBurningRates, solverMatrix, (600, 750));
        return solver;
    }

    public Individual Run()
    {
        return _optimizer.Run();
    }
}

public class FitnessFunctionInvoker : IFitnessFunctionInvoker
{
    private readonly BaseConstrainer _constrainer;
    private readonly ITargetFunctionSolver _solver;

    public FitnessFunctionInvoker(ITargetFunctionSolver solver,
                                  BaseConstrainer constrainer)
    {
        _solver = solver;
        _constrainer = constrainer;
    }

    public double Invoke(Span<double> point)
    {
        Span<double> values = stackalloc double[3];
        _solver.RunTargetFunction(point, values);

        if (values[0] == double.MaxValue)
            return double.MaxValue;

        var penaltyValue = _constrainer.GetPenaltyValue(point);

        return values[0] + penaltyValue;
    }

    public double GetTotalPenalty(Span<double> point)
    {
        Span<double> values = stackalloc double[3];
        _solver.RunTargetFunction(point, values);

        if (values[0] == double.MaxValue)
            return double.MaxValue;

        return _constrainer.GetPenaltyValue(point);
    }
}

public class CustomPopulationGenerator(int populationSize,
                                       ReadOnlyCollection<double> upperBound,
                                       ReadOnlyCollection<double> lowerBound,
                                       ReadOnlyCollection<double> startPoint) : IPopulationGenerator
{
    public Population Generate(IFitnessFunctionInvoker fitnessFunctionInvoker)
    {
        var random = new Random();
        var individuals = new Individual[populationSize];
        var vectors = new double[populationSize][];
        for (var i = 0; i < populationSize; i++)
        {
            vectors[i] = new double[lowerBound.Count];
            for (var j = 0; j < lowerBound.Count; j++)
            {
                var upperBoundByStartPoint = startPoint[j] * 1.1; // +10%
                var lowerBoundByStartPoint = startPoint[j] * 0.9; // -10%

                if (upperBoundByStartPoint > upperBound[j])
                    upperBoundByStartPoint = upperBound[j];

                if (lowerBoundByStartPoint > lowerBound[j])
                    lowerBoundByStartPoint = lowerBound[j];
                
                vectors[i][j] = random.NextDouble() * (upperBoundByStartPoint - lowerBoundByStartPoint) + lowerBoundByStartPoint;
            }

            var individual = new Individual(fitnessFunctionInvoker.Invoke(vectors[i]), vectors[i]);
            individuals[i] = individual;
        }

        return new Population(individuals.ToArray());
    }
}
