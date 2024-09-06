using System.Collections.ObjectModel;
using MatlabGlobalSearch.Api;
using MatlabGlobalSearch.Api.DTOs;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Core.DTOs;
using ParametricCombustionModel.Optimization.Events;
using ParametricCombustionModel.Optimization.Interfaces;
using ParametricCombustionModel.Optimization.TargetFunctions;
using ParametricCombustionModel.Optimization.TargetFunctions.Interfaces;
using ParametricCombustionModel.ParamsRecording.Builders;
using ParametricCombustionModel.ParamsRecording.ParamsRecorders;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Optimization;

public class GlobalSearchOptimizer : IParametricModelOptimizer
{
    public Task<OperationResult<OptimizationResult>> RunAsync(OptimizationContext context)
    {
        return GetOptimizationResultAsync(context);
    }

    private async Task<OperationResult<OptimizationResult>> GetOptimizationResultAsync(OptimizationContext context)
    {
        try
        {
            var optimizationResult = await TryGetOptimizationResultAsync(context);
            return new OperationResult<OptimizationResult>(optimizationResult);
        }
        catch (Exception ex)
        {
            return new OperationResult<OptimizationResult>(ex);
        }
    }

    private async Task<OptimizationResult> TryGetOptimizationResultAsync(OptimizationContext context)
    {
        var solverMatrix = GetMixedPropellantSolvers(context);
        var targetFunctionSolver = GetTargetFunctionSolver(context, solverMatrix);
        var settings = new OptimizerSettings(context.LowerBound.ToArray(),
                                             context.UpperBound.ToArray(),
                                             GetTargetFunctionInvoker(targetFunctionSolver, context),
                                             context.InitialPoint.ToArray(),
                                             GetNonlconFunctionInvoker(targetFunctionSolver, solverMatrix, context),
                                             GetOutputCallbackInvoker(targetFunctionSolver, context),
                                             NumStageOnePoints: context.NumStageOnePoints,
                                             NumTrialPoints: context.NumTrialPoints,
                                             MaxTime: (int?)context.MaxTime?.TotalSeconds ?? int.MaxValue);
        var optimizer = new GlobalSearchAdapter();
        var operationResult = await optimizer.RunAsync(settings);

        if (operationResult.IsSuccess == false || operationResult.Value is null)
            throw operationResult.Exception ?? throw new InvalidOperationException("Optimization fault.");

        var result = operationResult.Value;

        return new OptimizationResult(result.TargetFunctionValue, result.FinalPoint);
    }

    protected virtual ReadOnlyCollection<ReadOnlyCollection<MixedSolverParamsRecorder>> GetMixedPropellantSolvers(
        OptimizationContext context)
    {
        return MixedSolverParamsRecordersBuilder.FromPropellants(context.Propellants)
                                                .ForPressures(context.Pressures)
                                                .Build();
    }

    protected virtual ITargetFunctionSolver GetTargetFunctionSolver(OptimizationContext context,
                                                                    IEnumerable<IEnumerable<MixedPropellantSolver>>
                                                                        solverMatrix)
    {
        var experimentalBurningRates = context.Propellants.GetExperimentalBurnRates(context.Pressures);
        var solver = new TargetFunctionNonlconSolver(experimentalBurningRates,
                                                     solverMatrix.Select(x => x.ToArray().AsReadOnly())
                                                                 .ToArray()
                                                                 .AsReadOnly(),
                                                     (context.MinSurfaceTemperature, context.MaxSurfaceTemperature));
        return solver;
    }

    protected virtual Func<double[], double> GetTargetFunctionInvoker(ITargetFunctionSolver solver,
                                                                      OptimizationContext context)
    {
        Func<double[], double> targetFunctionInvoker = x =>
        {
            Span<double> values = stackalloc double[3];
            solver.RunTargetFunction(x, values);
            return values[0];
        };

        return targetFunctionInvoker;
    }

    protected virtual Func<double[], double> GetNonlconFunctionInvoker(ITargetFunctionSolver targetFunctionSolver,
                                                                       IEnumerable<IEnumerable<
                                                                           MixedSolverParamsRecorder>> solverMatrix,
                                                                       OptimizationContext context)
    {
        return point => -1;
    }

    protected virtual Func<double[], string, bool> GetOutputCallbackInvoker(
        ITargetFunctionSolver solver,
        OptimizationContext context)
    {
        Func<double[], string, bool> outputCallbackInvoker = (args, state) =>
        {
            var updatedEvent = new OptimizationUpdatedEvent
            {
                Args = args,
                State = Enum.Parse<State>(state)
            };

            EventBus<OptimizationUpdatedEvent>.Publish(updatedEvent);

            return false;
        };

        return outputCallbackInvoker;
    }
}
