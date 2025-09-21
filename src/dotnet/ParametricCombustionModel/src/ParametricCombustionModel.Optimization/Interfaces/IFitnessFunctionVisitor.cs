using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.Models;

namespace ParametricCombustionModel.Optimization.Interfaces;

public interface IFitnessFunctionVisitor
{
    public void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        OptimizationProblemByUnits context);

    public void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        OptimizationProblemByDoubles context);
}
