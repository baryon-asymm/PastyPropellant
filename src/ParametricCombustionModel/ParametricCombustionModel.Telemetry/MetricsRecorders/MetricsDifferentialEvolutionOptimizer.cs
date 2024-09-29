using ParametricCombustionModel.Computation.Extensions;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Core.DTOs;
using ParametricCombustionModel.Optimization;
using ParametricCombustionModel.Optimization.Extensions;
using ParametricCombustionModel.Optimization.FitnessFunctionEvaluators.Interfaces;
using ParametricCombustionModel.Optimization.Optimizers;

namespace ParametricCombustionModel.Telemetry.MetricsRecorders;

public class MetricsDifferentialEvolutionOptimizer : DifferentialEvolutionOptimizer
{
    public FitnessFunctionMetricsRecorder? TargetFunctionMetricsRecorder { get; private set; }

    protected override IFitnessFunctionEvaluator GetTargetFunctionSolver(OptimizationContext context,
                                                                     IEnumerable<IEnumerable<MixedPropellantSolver>>
                                                                         solverMatrix)
    {
        var experimentalBurningRates = context.Propellants.GetExperimentalBurnRates(context.Pressures);
        var solver = new FitnessFunctionMetricsRecorder(experimentalBurningRates,
                                                       solverMatrix,
                                                       (context.MinSurfaceTemperature, context.MaxSurfaceTemperature));
        TargetFunctionMetricsRecorder = solver;
        return TargetFunctionMetricsRecorder;
    }
}
