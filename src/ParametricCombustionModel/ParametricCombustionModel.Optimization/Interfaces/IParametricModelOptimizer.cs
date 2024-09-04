using ParametricCombustionModel.Core.DTOs;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Optimization.Interfaces;

public interface IParametricModelOptimizer
{
    public Task<OperationResult<OptimizationResult>> RunAsync(OptimizationContext context);
}
