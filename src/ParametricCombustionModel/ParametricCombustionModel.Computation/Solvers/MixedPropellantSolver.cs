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

    /// <summary>
    /// Initializes a new instance of the <see cref="MixedPropellantSolver"/> class, setting up the necessary solvers for 
    /// inter-pocket and pocket propellant regions.
    /// </summary>
    public MixedPropellantSolver()
    {
        _interPocketPropellantSolver = new InterPocketPropellantSolver();
        _pocketPropellantSolver = new PocketPropellantSolver();
    }

#endregion

#region Visit Methods

    /// <summary>
    /// Visits the <see cref="ProblemContextByUnits"/> with the provided <see cref="CombustionSolverParamsByUnits"/> to calculate the 
    /// combustion parameters for the mixed propellant.
    /// </summary>
    /// <param name="solverParamsByUnits">
    /// The parameters related to the combustion process, including enthalpy change and specific heat capacity.
    /// </param>
    /// <param name="context">
    /// The context that contains parameters for both the inter-pocket and pocket combustion models.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context)
    {
        ref var mixedContextBag = ref context.MixedCombustionParams;

        context.Accept(solverParamsByUnits, _interPocketPropellantSolver);
        ref var interPocketContextBag = ref context.InterPocketCombustionParams;
        if (interPocketContextBag.BurnRateIsFound)
        {
            context.Accept(solverParamsByUnits, _pocketPropellantSolver);
            ref var pocketContextBag = ref context.PocketCombustionParams;
            if (pocketContextBag.BurnRateIsFound)
            {
                mixedContextBag.BurnRate = GetBurnRate(context);
            }
        }

        mixedContextBag.BurnRateIsFound = interPocketContextBag.BurnRateIsFound
                                          && context.PocketCombustionParams.BurnRateIsFound;
    }

    /// <summary>
    /// Visits the <see cref="ProblemContextByDoubles"/> with the provided <see cref="CombustionSolverParamsByDoubles"/> to calculate the 
    /// combustion parameters for the mixed propellant.
    /// </summary>
    /// <param name="solverParams">
    /// The parameters related to the combustion process, including enthalpy change and specific heat capacity.
    /// </param>
    /// <param name="context">
    /// The context that contains parameters for both the inter-pocket and pocket combustion models.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context)
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
    /// <param name="context">
    /// The context that contains parameters for both the inter-pocket and pocket combustion models, including volume fractions and burn rates.
    /// </param>
    /// <returns>
    /// The burn rate of the mixed propellant as a <see cref="Speed"/> object, representing the rate in meters per second.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

#region Computation Methods with Double Parameters

    /// <summary>
    /// Calculates the burn rate of the mixed propellant based on the surface temperatures of both regions and burn parameters.
    /// </summary>
    /// <param name="context">
    /// The context that contains parameters for both the inter-pocket and pocket combustion models, including volume fractions and burn rates.
    /// </param>
    /// <returns>
    /// The burn rate of the mixed propellant as a <see cref="double"/> value, representing the rate in meters per second.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private double GetBurnRate(
        ProblemContextByDoubles context)
    {
        ref var interPocketContextBag = ref context.InterPocketCombustionParams;
        ref var pocketContextBag = ref context.PocketCombustionParams;

        var mixedPropellantBurnRate =
            1.0
            / (
                  context.InterPocketVolumeFraction / interPocketContextBag.BurnRate
                  + context.PocketVolumeFraction / pocketContextBag.BurnRate
              );

        return mixedPropellantBurnRate;
    }

#endregion
}
