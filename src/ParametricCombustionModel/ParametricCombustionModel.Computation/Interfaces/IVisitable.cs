using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Computation.Interfaces;

/// <summary>
/// Defines an interface for objects that can be "visited" by a solver.
/// This is used to apply different solvers to combustion processes using either
/// UnitsNet types or native double values for the parameters.
/// </summary>
public interface IVisitable
{
    /// <summary>
    /// Accepts a solver using combustion parameters based on UnitsNet types.
    /// This method applies the provided solver to the current context using the given combustion parameters.
    /// </summary>
    /// <param name="solverParams">The combustion solver parameters, represented using UnitsNet types.</param>
    /// <param name="solver">The solver that implements the <see cref="ISolverVisitor"/> interface to process the parameters.</param>
    public void Accept(
        in CombustionSolverParams solverParams,
        ISolverVisitor solver);

    /// <summary>
    /// Accepts a solver using combustion parameters based on native double values.
    /// This method applies the provided solver to the current context using the given combustion parameters.
    /// </summary>
    /// <param name="solverParams">The combustion solver parameters, represented using native double values.</param>
    /// <param name="solver">The solver that implements the <see cref="ISolverVisitor"/> interface to process the parameters.</param>
    public void Accept(
        in CombustionSolverParamsByDoubles solverParams,
        ISolverVisitor solver);
}
