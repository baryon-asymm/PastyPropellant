using System.Collections.ObjectModel;
using System.Text.Json;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Core.Models;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators;
using ParametricCombustionModel.Optimization.ConstraintPenaltyEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.FitnessFunctionEvaluators;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.Models;
using PastyPropellant.Core.Utils;
using UnitsNet;

namespace ParametricCombustionModel.ProcessWorker.Scenarios;

public class ReportPdfPrintScenario
{
    public IFitnessFunctionVisitor FitnessFunctionEvaluator { get; init; }

    public OptimizationProblemByUnits OptimizationProblemContext { get; init; }

    private readonly ReadOnlyMemory<double> _lowerBound;
    private readonly ReadOnlyMemory<double> _upperBound;

    public ReportPdfPrintScenario(
        string propellantsFilePath,
        ReadOnlyMemory<double> lowerBound,
        ReadOnlyMemory<double> upperBound)
    {
        var propellants = GetPropellants(propellantsFilePath);
        var penaltyEvaluators = GetPenaltyEvaluators();
        var contextMatrixByUnits = GetProblemContextMatrixByUnits(propellants);

        OptimizationProblemContext = GetOptimizationProblemContextByUnits(
            contextMatrixByUnits,
            penaltyEvaluators);
        FitnessFunctionEvaluator = GetTargetFunctionSolver();

        _lowerBound = lowerBound;
        _upperBound = upperBound;
    }

    private ReadOnlyCollection<Propellant> GetPropellants(
        string filePath)
    {
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = JsonSerializer.DeserializeAsync<List<Propellant>>(fileStream).Result
            ?? throw new InvalidOperationException("Failed to deserialize propellants from file.");
        return result.AsReadOnly();
    }

    private IEnumerable<IPenaltyEvaluator> GetPenaltyEvaluators()
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
                                                     maxOutSkeletonKineticFlameHeatFlux)
        };

        return penaltyEvaluators;
    }

    private ProblemContextByUnits[,] GetProblemContextMatrixByUnits(
        IEnumerable<Propellant> propellants)
    {
        var contextMatrix = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants)
                                                              .BuildMatrix();

        return contextMatrix;
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

    public Task<OperationResult<OptimizationResult>> RunAsync(
        Span<double> point)
    {
        var solverParams = CombustionSolverParamsByUnits.FromVector(point);

        OptimizationProblemContext.Accept(solverParams, FitnessFunctionEvaluator);

        return Task.FromResult(new OperationResult<OptimizationResult>(
                                   new OptimizationResult(
                                    lowerBound: _lowerBound.ToArray(),
                                    upperBound: _upperBound.ToArray(),
                                    bestParams: point.ToArray(), OptimizationProblemContext)));
    }
}
