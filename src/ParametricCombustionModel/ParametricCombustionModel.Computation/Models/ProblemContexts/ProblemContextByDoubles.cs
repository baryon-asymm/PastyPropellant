using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Computation.Models.ProblemContexts;

/// <summary>
/// Represents the problem context for combustion modeling using native double values for physical parameters.
/// This class holds various combustion-related parameters and supports the visitor pattern for applying solvers.
/// </summary>
public class ProblemContextByDoubles : IVisitable
{
#region Fields

    /// <summary>
    /// Gets or sets the pressure within the combustion context.
    /// Measured in Pascals (Pa).
    /// </summary>
    public required double Pressure;

    /// <summary>
    /// Gets or sets the propellant parameters for the current context.
    /// These parameters define the material properties of the propellant, using double values.
    /// </summary>
    public required PropellantParamsByDoubles PropellantParams;

    /// <summary>
    /// Gets or sets the kinetic flame parameters for the inter-pocket combustion process, using double values.
    /// </summary>
    public required KineticFlameParamsByDoubles InterPocketKineticFlameParams;

    /// <summary>
    /// Gets or sets the kinetic flame parameters for the skeleton combustion process inside a pocket, using double values.
    /// </summary>
    public required KineticFlameParamsByDoubles PocketSkeletonKineticFlameParams;

    /// <summary>
    /// Gets or sets the kinetic flame parameters for the combustion process outside the skeleton in a pocket, using double values.
    /// </summary>
    public required KineticFlameParamsByDoubles PocketOutSkeletonKineticFlameParams;

    /// <summary>
    /// Gets or sets the diffusion flame parameters for the combustion process in a pocket, using double values.
    /// </summary>
    public required DiffusionFlameParamsByDoubles PocketDiffusionFlameParams;

    /// <summary>
    /// Gets or sets the metal combustion parameters for the pocket combustion process, using double values.
    /// </summary>
    public required MetalCombustionParamsByDoubles PocketMetalCombustionParams;

    /// <summary>
    /// Gets or sets the volume fraction of inter-pocket combustion.
    /// Represents the ratio of volume occupied by the inter-pocket combustion zone.
    /// </summary>
    public required double InterPocketVolumeFraction;

    /// <summary>
    /// Gets or sets the volume fraction of the combustion process inside the pocket.
    /// Represents the ratio of volume occupied by the pocket combustion zone.
    /// </summary>
    public required double PocketVolumeFraction;

    /// <summary>
    /// Gets or sets the mixed combustion parameters for the current context, using double values.
    /// </summary>
    public required MixedCombustionParamsByDoubles MixedCombustionParams;

    /// <summary>
    /// Gets or sets the inter-pocket combustion parameters for the current context, using double values.
    /// </summary>
    public required InterPocketCombustionParamsByDoubles InterPocketCombustionParams;

    /// <summary>
    /// Gets or sets the pocket combustion parameters for the current context, using double values.
    /// </summary>
    public required PocketCombustionParamsByDoubles PocketCombustionParams;

#endregion

#region Accept Methods

    /// <summary>
    /// Throws an exception, as the method is not supported for UnitsNet-based parameters in this context.
    /// </summary>
    /// <param name="solverParams">The solver parameters, represented by UnitsNet types.</param>
    /// <param name="solver">The solver implementing the <see cref="ISolverVisitor"/> interface.</param>
    /// <exception cref="NotSupportedException">Thrown because this method does not support UnitsNet-based parameters.</exception>
    public void Accept(
        in CombustionSolverParams solverParams,
        ISolverVisitor solver) =>
        throw new NotSupportedException("This method is not supported.");

    /// <summary>
    /// Accepts a solver that operates on the problem context using double values for the parameters.
    /// This method uses aggressive optimization to improve performance.
    /// </summary>
    /// <param name="solverParams">The solver parameters, represented by native double values.</param>
    /// <param name="solver">The solver implementing the <see cref="ISolverVisitor"/> interface.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Accept(
        in CombustionSolverParamsByDoubles solverParams,
        ISolverVisitor solver) =>
        solver.Visit(solverParams, this);

#endregion
}
