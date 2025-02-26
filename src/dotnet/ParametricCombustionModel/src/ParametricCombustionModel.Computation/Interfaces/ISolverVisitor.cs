using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;

namespace ParametricCombustionModel.Computation.Interfaces;

/// <summary>
/// Defines an interface for solvers that "visit" problem contexts and apply computational models to them.
/// The visitor processes combustion problem contexts using either UnitsNet types or native double values for parameters.
/// </summary>
public interface ISolverVisitor
{
    /// <summary>
    /// Processes a problem context using combustion parameters represented by UnitsNet types.
    /// This method applies the computational solver to the given problem context.
    /// </summary>
    /// <param name="solverParamsByUnits">The combustion solver parameters, represented using UnitsNet types.</param>
    /// <param name="context">The problem context, defined using UnitsNet types for combustion parameters.</param>
    public void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context);

    /// <summary>
    /// Processes a problem context using combustion parameters represented by native double values.
    /// This method applies the computational solver to the given problem context.
    /// </summary>
    /// <param name="solverParams">The combustion solver parameters, represented using native double values.</param>
    /// <param name="context">The problem context, defined using native double values for combustion parameters.</param>
    public void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context);
}
