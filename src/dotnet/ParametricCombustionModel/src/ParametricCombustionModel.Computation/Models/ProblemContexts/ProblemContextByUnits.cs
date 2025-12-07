using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Core.Models;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ProblemContexts;

/// <summary>
/// Represents the problem context for combustion modeling using UnitsNet types for physical parameters.
/// This class holds various combustion-related parameters and supports the visitor pattern for applying solvers.
/// </summary>
public record ProblemContextByUnits : IComputationVisitable
{
#region Fields

    /// <summary>
    /// The propellant being used in this problem context.
    /// </summary>
    public required Propellant Propellant;

    /// <summary>
    /// Gets or sets the pressure within the combustion context.
    /// Measured in units of pressure (Pa).
    /// </summary>
    public required Pressure Pressure;

    /// <summary>
    /// Gets or sets the propellant parameters for the current context.
    /// These parameters define the material properties of the propellant.
    /// </summary>
    public required PropellantParamsByUnits PropellantParamsByUnits;

    /// <summary>
    /// Gets or sets the kinetic flame parameters for the inter-pocket combustion process.
    /// </summary>
    public required KineticFlameParamsByUnits InterPocketKineticFlameParamsByUnits;

    /// <summary>
    /// Gets or sets the kinetic flame parameters for the skeleton combustion process inside a pocket.
    /// </summary>
    public required KineticFlameParamsByUnits PocketSkeletonKineticFlameParamsByUnits;

    /// <summary>
    /// Gets or sets the kinetic flame parameters for the combustion process outside the skeleton in a pocket.
    /// </summary>
    public required KineticFlameParamsByUnits PocketOutSkeletonKineticFlameParamsByUnits;

    /// <summary>
    /// Gets or sets the diffusion flame parameters for the combustion process in a pocket.
    /// </summary>
    public required DiffusionFlameParamsByUnits PocketDiffusionFlameParamsByUnits;

    /// <summary>
    /// Gets or sets the metal combustion parameters for the pocket combustion process.
    /// </summary>
    public required MetalCombustionParamsByUnits PocketMetalCombustionParamsByUnits;

    public required SkeletonLayerParamsByUnits SkeletonLayerParamsByUnits;

    /// <summary>
    /// Gets or sets the volume fraction of inter-pocket combustion.
    /// Represents the ratio of volume occupied by the inter-pocket combustion zone.
    /// </summary>
    public required Ratio InterPocketVolumeFraction;

    /// <summary>
    /// Gets or sets the volume fraction of the combustion process inside the pocket.
    /// Represents the ratio of volume occupied by the pocket combustion zone.
    /// </summary>
    public required Ratio PocketVolumeFraction;

    /// <summary>
    /// Gets or sets the mixed combustion parameters for the current context.
    /// </summary>
    public required MixedCombustionParams MixedCombustionParams;

    /// <summary>
    /// Gets or sets the inter-pocket combustion parameters for the current context.
    /// </summary>
    public required InterPocketCombustionParams InterPocketCombustionParams;

    /// <summary>
    /// Gets or sets the pocket combustion parameters for the current context.
    /// </summary>
    public required PocketCombustionParams PocketCombustionParams;

#endregion

#region Parametric Constraints

    /// <summary>
    /// The minimum surface temperature for the propellant combustion process.
    /// This property is used as the lower bound in binary search algorithms for solving the transcendental equation
    /// to find the surface temperature of the propellant (condensed phase).
    /// </summary>
    public Temperature MinSurfaceTemperature = Temperature.FromKelvins(599);

    /// <summary>
    /// The maximum surface temperature for the propellant combustion process.
    /// This property is used as the upper bound in binary search algorithms for solving the transcendental equation
    /// to find the surface temperature of the propellant (condensed phase).
    /// </summary>
    public Temperature MaxSurfaceTemperature = Temperature.FromKelvins(751);

#endregion

#region Accept Methods

    /// <summary>
    /// Accepts a solver that operates on the problem context using UnitsNet types for the parameters.
    /// This method uses aggressive optimization to improve performance.
    /// </summary>
    /// <param name="solverParamsByUnits">The solver parameters, represented by UnitsNet types.</param>
    /// <param name="solver">The solver implementing the <see cref="ISolverVisitor"/> interface.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Accept(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ISolverVisitor solver) =>
        solver.Visit(solverParamsByUnits, this);

    /// <summary>
    /// Throws an exception, as the method is not supported for native double parameters in this context.
    /// </summary>
    /// <param name="solverParams">The solver parameters, represented by native double types.</param>
    /// <param name="solver">The solver implementing the <see cref="ISolverVisitor"/> interface.</param>
    /// <exception cref="NotSupportedException">Thrown because this method does not support double-based parameters.</exception>
    public void Accept(
        in CombustionSolverParamsByDoubles solverParams,
        ISolverVisitor solver) =>
        throw new NotSupportedException("This method is not supported.");

#endregion
}
