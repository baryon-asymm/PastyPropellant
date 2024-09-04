using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Solvers;

/// <summary>
/// The <see cref="MixedPropellantSolver"/> class is responsible for calculating combustion parameters of a mixed propellant,
/// which includes both inter-pocket and pocket propellant regions.
/// It combines the properties of these two regions to determine burn rates and surface temperatures.
/// </summary>
public sealed class MixedPropellantSolver : ISolverVisitor
{
#region Fields

    /// <summary>
    /// Solver for the inter-pocket propellant region, responsible for computing combustion parameters such as burn rate and surface temperature
    /// specific to the inter-pocket area.
    /// </summary>
    private readonly InterPocketPropellantSolver _interPocketPropellantSolver;

    /// <summary>
    /// Solver for the pocket propellant region, responsible for computing combustion parameters such as burn rate and surface temperature
    /// specific to the pocket area.
    /// </summary>
    private readonly PocketPropellantSolver _pocketPropellantSolver;

#endregion

#region Constructors

    public MixedPropellantSolver()
    {
        _interPocketPropellantSolver = new InterPocketPropellantSolver();
        _pocketPropellantSolver = new PocketPropellantSolver();
    }

#endregion

#region Visit Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Visit(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
    {
        ref var mixedContextBag = ref context.MixedCombustionParams;

        context.Accept(solverParams, _interPocketPropellantSolver);
        ref var interPocketContextBag = ref context.InterPocketCombustionParams;
        if (interPocketContextBag.BurnRateIsFound)
        {
            context.Accept(solverParams, _pocketPropellantSolver);
            ref var pocketContextBag = ref context.PocketCombustionParams;
            if (pocketContextBag.BurnRateIsFound)
            {
                mixedContextBag.BurnRate = GetBurnRate(context);
            }
        }

        mixedContextBag.BurnRateIsFound = interPocketContextBag.BurnRateIsFound
                                          && context.PocketCombustionParams.BurnRateIsFound;
    }

#endregion

#region Computation Methods

    /// <summary>
    /// Calculates the burn rate of the mixed propellant based on the surface temperatures of both regions and burn parameters.
    /// </summary>
    /// <param name="interPocketSurfaceTemperature">The surface temperature for the inter-pocket region.</param>
    /// <param name="pocketSurfaceTemperature">The surface temperature for the pocket region.</param>
    /// <param name="solverParams">The parameters related to the burn process, including the enthalpy change and specific heat capacity.</param>
    /// <returns>The burn rate of the mixed propellant as a <see cref="Speed"/> object.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Speed GetBurnRate(
        ProblemContextByUnits context)
    {
        ref var interPocketContextBag = ref context.InterPocketCombustionParams;
        ref var pocketContextBag = ref context.PocketCombustionParams;

        var mixedPropellantBurnRate = Speed.FromMetersPerSecond(
            1.0
            / (
                  context.InterPocketVolumeFraction.DecimalFractions / interPocketContextBag.BurnRate.MetersPerSecond
                  + context.PocketVolumeFraction.DecimalFractions / pocketContextBag.BurnRate.MetersPerSecond
              ));

        return mixedPropellantBurnRate;
    }

#endregion
}
