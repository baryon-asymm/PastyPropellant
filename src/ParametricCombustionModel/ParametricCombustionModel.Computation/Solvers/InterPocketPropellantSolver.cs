using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Solvers;

/// <summary>
/// A concrete implementation of <see cref="BaseKineticPropellantSolver"/> for solving kinetic propellant combustion problems
/// specific to the InterPocket combustion model. This class handles the computation of heat fluxes, surface temperatures,
/// and extraction of kinetic flame parameters.
/// </summary>
public sealed class InterPocketPropellantSolver : BaseKineticPropellantSolver
{
#region Overridden Methods

    /// <summary>
    /// Visits a problem context using <see cref="CombustionSolverParamsByUnits"/> and updates the context with the computed
    /// surface temperature, burn rate, and heat flux errors.
    /// </summary>
    /// <param name="solverParamsByUnits">The parameters related to the combustion process, provided as <see cref="CombustionSolverParamsByUnits"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByUnits"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Visit(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context)
    {
        ref var contextBag = ref context.InterPocketCombustionParams;

        contextBag.BurnRateIsFound = TryGetSurfaceTemperature(solverParamsByUnits, context, out contextBag.SurfaceTemperature);

        if (contextBag.BurnRateIsFound)
        {
            // Update the context bags with the latest surface temperature
            contextBag.SurfaceHeatFluxesError = GetSurfaceHeatFluxesError(contextBag.SurfaceTemperature,
                                                                          solverParamsByUnits,
                                                                          context);
            // Get the burn rate using the updated context bags
            contextBag.BurnRate = GetBurnRate(contextBag.DecomposeRate, context.PropellantParamsByUnits);
        }
    }

    /// <summary>
    /// Calculates the error in heat fluxes at the propellant surface by comparing the kinetic flame heat flux
    /// with the sublimation heat flux. This method uses the surface temperature, pressure, and burn parameters
    /// to compute the heat fluxes and their difference.
    /// </summary>
    /// <param name="surfaceTemperature">The temperature of the propellant surface, provided as <see cref="Temperature"/>.</param>
    /// <param name="solverParamsByUnits">The parameters related to the burn process, including the enthalpy change and specific heat capacity, provided as <see cref="CombustionSolverParamsByUnits"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByUnits"/>.</param>
    /// <returns>
    /// The difference between the kinetic flame heat flux and the sublimation heat flux, as a <see cref="HeatFlux"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        ProblemContextByUnits context)
    {
        var deltaH = solverParamsByUnits.DeltaH;
        var specificHeatCapacity = context.PropellantParamsByUnits.SpecificHeatCapacity;
        var initialTemperature = context.PropellantParamsByUnits.InitialTemperature;

        ref var contextBag = ref context.InterPocketCombustionParams;
        ref var kineticContextBag = ref contextBag.KineticFlameCombustionParams;

        contextBag.DecomposeRate = GetDecomposeRate(surfaceTemperature,
                                                    solverParamsByUnits);
        kineticContextBag.KineticFlameHeatFlux = GetKineticFlameHeatFlux(context.Pressure,
                                                                         surfaceTemperature,
                                                                         contextBag.DecomposeRate,
                                                                         solverParamsByUnits,
                                                                         context.InterPocketKineticFlameParamsByUnits,
                                                                         ref kineticContextBag);

        var enthalpyChange = specificHeatCapacity
                             * (surfaceTemperature - initialTemperature)
                             + deltaH;
        contextBag.SublimationHeatFlux = HeatFlux.FromWattsPerSquareMeter(
            contextBag.DecomposeRate.KilogramsPerSecondPerSquareMeter
            * enthalpyChange.JoulesPerKilogram);

        return kineticContextBag.KineticFlameHeatFlux - contextBag.SublimationHeatFlux;
    }

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the InterPocket model from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame.
    /// </summary>
    /// <param name="solverParamsByUnits">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByUnits"/>.</param>
    /// <param name="aKineticFlame">The pre-exponential factor for the kinetic flame, returned as a <see cref="Frequency"/> object.</param>
    /// <param name="eKineticFlame">The activation energy for the kinetic flame, returned as a <see cref="MolarEnergy"/> object.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        out Frequency aKineticFlame,
        out MolarEnergy eKineticFlame)
    {
        aKineticFlame = solverParamsByUnits.AKineticFlameInterPocket;
        eKineticFlame = solverParamsByUnits.EKineticFlameInterPocket;
    }

#endregion

#region Overridden Methods with Double Parameters

    /// <summary>
    /// Visits a problem context using <see cref="CombustionSolverParamsByDoubles"/> and updates the context with the computed
    /// surface temperature, burn rate, and heat flux errors.
    /// </summary>
    /// <param name="solverParams">The parameters related to the combustion process, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByDoubles"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Visit(
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context)
    {
        ref var contextBag = ref context.InterPocketCombustionParams;

        contextBag.BurnRateIsFound = TryGetSurfaceTemperature(solverParams, context, out contextBag.SurfaceTemperature);

        if (contextBag.BurnRateIsFound)
        {
            // Update the context bags with the latest surface temperature
            contextBag.SurfaceHeatFluxesError = GetSurfaceHeatFluxesError(contextBag.SurfaceTemperature,
                                                                          solverParams,
                                                                          context);
            // Get the burn rate using the updated context bags
            contextBag.BurnRate = GetBurnRate(contextBag.DecomposeRate, context.PropellantParams);

            contextBag.BurnRateIsFound = contextBag.BurnRate > 0.0;
        }
    }

    /// <summary>
    /// Calculates the error in heat fluxes at the propellant surface by comparing the kinetic flame heat flux
    /// with the sublimation heat flux. This method uses the surface temperature, pressure, and burn parameters
    /// to compute the heat fluxes and their difference.
    /// </summary>
    /// <param name="surfaceTemperature">The temperature of the propellant surface, provided as a double.</param>
    /// <param name="solverParams">The parameters related to the burn process, including the enthalpy change and specific heat capacity, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <param name="context">The problem context, provided as <see cref="ProblemContextByDoubles"/>.</param>
    /// <returns>
    /// The difference between the kinetic flame heat flux and the sublimation heat flux, as a double.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override double GetSurfaceHeatFluxesError(
        double surfaceTemperature,
        in CombustionSolverParamsByDoubles solverParams,
        ProblemContextByDoubles context)
    {
        var deltaH = solverParams.DeltaH;
        var specificHeatCapacity = context.PropellantParams.SpecificHeatCapacity;
        var initialTemperature = context.PropellantParams.InitialTemperature;

        ref var contextBag = ref context.InterPocketCombustionParams;
        ref var kineticContextBag = ref contextBag.KineticFlameCombustionParams;

        contextBag.DecomposeRate = GetDecomposeRate(surfaceTemperature,
                                                    solverParams);
        kineticContextBag.KineticFlameHeatFlux = GetKineticFlameHeatFlux(context.Pressure,
                                                                         surfaceTemperature,
                                                                         contextBag.DecomposeRate,
                                                                         solverParams,
                                                                         context.InterPocketKineticFlameParams,
                                                                         ref kineticContextBag);

        var enthalpyChange = specificHeatCapacity
                             * (surfaceTemperature - initialTemperature)
                             + deltaH;
        contextBag.SublimationHeatFlux =
            contextBag.DecomposeRate
            * enthalpyChange;

        return kineticContextBag.KineticFlameHeatFlux - contextBag.SublimationHeatFlux;
    }

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the InterPocket model from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame.
    /// </summary>
    /// <param name="solverParams">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <param name="aKineticFlame">The pre-exponential factor for the kinetic flame, returned as a double.</param>
    /// <param name="eKineticFlame">The activation energy for the kinetic flame, returned as a double.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParamsByDoubles solverParams,
        out double aKineticFlame,
        out double eKineticFlame)
    {
        aKineticFlame = solverParams.AKineticFlameInterPocket;
        eKineticFlame = solverParams.EKineticFlameInterPocket;
    }

#endregion
}
