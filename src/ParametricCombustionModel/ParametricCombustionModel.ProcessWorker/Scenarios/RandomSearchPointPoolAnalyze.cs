using System.Collections.ObjectModel;
using System.Text.Json;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.DTOs;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.TargetFunctions;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Builders;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;

namespace ParametricCombustionModel.ProcessWorker.Scenarios;

public struct AnalysisResult
{
    public double MinFitnessFunctionCost { get; set; }
    public double MaxFitnessFunctionCost { get; set; }
    public double MeanFitnessFunctionCost { get; set; }
    public double StdDevFitnessFunctionCost { get; set; }
}

public class RandomSearchPointPoolAnalyze
{
    public AnalysisResult GetAnalysisResult(string filePath)
    {
        Span<double> vector = stackalloc double[11];
        Span<double> values = stackalloc double[3];

        var minFitnessFunctionCost = double.MaxValue;
        var maxFitnessFunctionCost = double.MinValue;
        double meanFitnessFunctionCost = 0;
        double dispersionFitnessFunctionCost = 0;

        using var fileReader = new FileStream(filePath, FileMode.Open);
        using var reader = new StreamReader(fileReader);

        for (var i = 0; i < 1500; i++)
        {
            var numsStr = reader.ReadLine().Split(' ');
            for (var j = 0; j < vector.Length; j++) vector[j] = double.Parse(numsStr[j]);

            var context = GetOptimizationContext("../data/propellant_bas_1.json");
            var solver = GetTargetFunctionSolver(context, GetMixedPropellantSolvers(context));
            solver.RunTargetFunction(vector, values);

            if (values[0] > 1e3)
                throw new Exception("Invalid fitness function value");

            var fitnessFunctionCost = values[0];
            minFitnessFunctionCost = Math.Min(minFitnessFunctionCost, fitnessFunctionCost);
            maxFitnessFunctionCost = Math.Max(maxFitnessFunctionCost, fitnessFunctionCost);

            var prevMeanFitnessFunctionCost = meanFitnessFunctionCost;
            meanFitnessFunctionCost += (fitnessFunctionCost - prevMeanFitnessFunctionCost) / (i + 1);
            dispersionFitnessFunctionCost +=
                ((fitnessFunctionCost - prevMeanFitnessFunctionCost) * (fitnessFunctionCost - meanFitnessFunctionCost) -
                 dispersionFitnessFunctionCost) / (i + 1);
        }

        var result = new AnalysisResult
        {
            MinFitnessFunctionCost = minFitnessFunctionCost,
            MaxFitnessFunctionCost = maxFitnessFunctionCost,
            MeanFitnessFunctionCost = meanFitnessFunctionCost,
            StdDevFitnessFunctionCost = Math.Sqrt(dispersionFitnessFunctionCost)
        };

        return result;
    }

    private OptimizationContext GetOptimizationContext(string propellantsFilePath)
    {
        var propellants = GetPropellants(propellantsFilePath);
        double[] pressures = [1.2e6, 1.9e6, 2.7e6, 3.4e6, 4.1e6, 4.9e6, 5.6e6, 6.3e6, 7.1e6, 7.8e6];
        return new OptimizationContext(pressures, null, 0, 0, default, default, 600, 750, propellants);
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
        var experimentalBurningRates = context.Propellants.GetExperimentalBurnRates(context.Pressures);
        var solver = new TargetFunctionNonlconSolver(experimentalBurningRates,
                                                     solverMatrix,
                                                     (context.MinSurfaceTemperature, context.MaxSurfaceTemperature));
        return solver;
    }
}
