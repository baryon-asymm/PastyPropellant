using System.Collections.ObjectModel;
using System.Text.Json;
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

public class RandomSearchPointPool
{
    private readonly PocketHeatFlowOffsetConstrainer _constrainer;

    private readonly ReadOnlyCollection<double> _lowerBound;

    private readonly double[][] _pool;
    private readonly int _poolSize;

    private readonly ITargetFunctionSolver _solver;
    private readonly ReadOnlyCollection<double> _upperBound;

    public RandomSearchPointPool(int poolSize,
                                 IEnumerable<double> lowerBound,
                                 IEnumerable<double> upperBound,
                                 int heatFlowsSegmentSize)
    {
        _poolSize = poolSize;
        _lowerBound = lowerBound.ToList().AsReadOnly();
        _upperBound = upperBound.ToList().AsReadOnly();

        if (_lowerBound.Count != _upperBound.Count)
            throw new ArgumentException("Lower and upper bounds must have the same length");

        _pool = new double[_poolSize][];
        for (var i = 0; i < _poolSize; i++) _pool[i] = new double[_lowerBound.Count];

        var context = GetOptimizationContext("../data/propellant_bas_1.json");
        var solverMatrix = GetMixedPropellantSolvers(context);
        _solver = GetTargetFunctionSolver(context, solverMatrix);
        _constrainer = new PocketHeatFlowOffsetConstrainer(heatFlowsSegmentSize, 1e2, solverMatrix, _solver);
    }

    private OptimizationContext GetOptimizationContext(string propellantsFilePath)
    {
        var propellants = GetPropellants(propellantsFilePath);
        double[] pressures = [1.2e6, 1.9e6, 2.7e6, 3.4e6, 4.1e6, 4.9e6, 5.6e6, 6.3e6, 7.1e6, 7.8e6];
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
        var solver = new TargetFunctionNonlconSolver(experimentalBurningRates,
                                                     solverMatrix,
                                                     (context.MinSurfaceTemperature, context.MaxSurfaceTemperature));
        return solver;
    }

    public async Task<ReadOnlyCollection<ReadOnlyCollection<double>>> GetPoolAsync()
    {
        await Parallel.ForAsync(0,
                                _poolSize,
                                (i, token) =>
                                {
                                    var random = Random.Shared;
                                    var isSolutionExists = false;

                                    Span<double> values = stackalloc double[3];

                                    while (isSolutionExists == false)
                                    {
                                        for (var j = 0; j < _lowerBound.Count; j++)
                                            _pool[i][j] = random.NextDouble() * (_upperBound[j] - _lowerBound[j]) +
                                                          _lowerBound[j];

                                        _solver.RunTargetFunction(_pool[i].AsSpan(), values);
                                        if (values[0] != double.MaxValue
                                            && _constrainer.GetPenaltyValue(_pool[i].AsSpan()) ==
                                            BaseConstrainer.ZeroPenaltyValue)
                                        {
                                            isSolutionExists = true;
                                            var msg = $"index {i} result {values[0]}";
                                            EventBus<string>.Publish(msg);
                                        }
                                    }

                                    return ValueTask.CompletedTask;
                                });

        return _pool.Select(x => x.ToArray().AsReadOnly()).ToArray().AsReadOnly();
    }
}
