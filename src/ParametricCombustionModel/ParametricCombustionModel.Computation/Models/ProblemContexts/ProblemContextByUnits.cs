using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ProblemContexts;

public record ProblemContextByUnits : IVisitable
{
#region Fields

    public required Pressure Pressure;
    public required PropellantParams PropellantParams;

    public required KineticFlameParams InterPocketKineticFlameParams;

    public required KineticFlameParams PocketSkeletonKineticFlameParams;
    public required KineticFlameParams PocketOutSkeletonKineticFlameParams;
    public required DiffusionFlameParams PocketDiffusionFlameParams;
    public required MetalCombustionParams PocketMetalCombustionParams;

    public required Ratio InterPocketVolumeFraction;
    public required Ratio PocketVolumeFraction;

    public required MixedCombustionParams MixedCombustionParams;
    public required InterPocketCombustionParams InterPocketCombustionParams;
    public required PocketCombustionParams PocketCombustionParams;

#endregion

#region Accept Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accept(
        in CombustionSolverParams solverParams,
        ISolverVisitor solver) =>
        solver.Visit(solverParams, this);

#endregion
}
