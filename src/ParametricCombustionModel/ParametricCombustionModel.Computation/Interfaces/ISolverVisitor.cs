using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;

namespace ParametricCombustionModel.Computation.Interfaces;

public interface ISolverVisitor
{
    public void Visit(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context);
}
