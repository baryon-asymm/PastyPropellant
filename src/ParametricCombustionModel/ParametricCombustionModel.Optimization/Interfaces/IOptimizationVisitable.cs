using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Optimization.Interfaces;

public interface IOptimizationVisitable
{
    public void Accept(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        IFitnessFunctionVisitor fitnessFunction);

    public void Accept(
        in CombustionSolverParamsByDoubles solverParams,
        IFitnessFunctionVisitor fitnessFunction);
}
