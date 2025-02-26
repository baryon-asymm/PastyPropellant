using ParametricCombustionModel.Optimization.Models;
using PastyPropellant.Core.Utils;

namespace ParametricCombustionModel.Optimization.Interfaces;

public interface IParametricCombustionModelOptimizer
{
    public Task<OperationResult<OptimizationResult>> RunAsync();
}
