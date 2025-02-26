using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.Models;

namespace ParametricCombustionModel.Optimization.Interfaces;

public interface IFitnessFunctionVisitor
{
    public void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        OptimizationProblemContextByUnits context);

    public void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        OptimizationProblemContextByDoubles context);
}
