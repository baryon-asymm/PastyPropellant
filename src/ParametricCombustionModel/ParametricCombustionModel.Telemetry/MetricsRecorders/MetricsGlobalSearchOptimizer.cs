using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.DTOs;
using ParametricCombustionModel.Optimization;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;

namespace ParametricCombustionModel.Telemetry.MetricsRecorders;

public class MetricsGlobalSearchOptimizer : GlobalSearchOptimizer
{
    public TargetFunctionMetricsRecorder? TargetFunctionMetricsRecorder { get; private set; }

    protected override ITargetFunctionSolver GetTargetFunctionSolver(OptimizationContext context,
                                                                     IEnumerable<IEnumerable<MixedPropellantSolver>>
                                                                         solverMatrix)
    {
        var experimentalBurningRates = context.Propellants.GetExperimentalBurnRates(context.Pressures);
        var solver = new TargetFunctionMetricsRecorder(experimentalBurningRates,
                                                       solverMatrix,
                                                       (context.MinSurfaceTemperature, context.MaxSurfaceTemperature));
        TargetFunctionMetricsRecorder = solver;
        return TargetFunctionMetricsRecorder;
    }
}
