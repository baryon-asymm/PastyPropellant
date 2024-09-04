using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Computation.Interfaces;

public interface IVisitable
{
    public void Accept(
        in CombustionSolverParams solverParams,
        ISolverVisitor solver);
}
