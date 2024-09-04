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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Visit(
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
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
        }
    }

    /// <summary>
    /// Calculates the error in heat fluxes at the propellant surface by comparing the kinetic flame heat flux
    /// with the sublimation heat flux. This method uses the surface temperature, pressure, and burn parameters
    /// to compute the heat fluxes and their difference.
    /// </summary>
    /// <param name="pressure">
    /// The pressure in the combustion chamber of the rocket engine.
    /// </param>
    /// <param name="surfaceTemperature">
    /// The temperature of the propellant surface.
    /// </param>
    /// <param name="solverParams">
    /// The parameters related to the burn process, including the enthalpy change and specific heat capacity.
    /// </param>
    /// <returns>
    /// The difference between the kinetic flame heat flux and the sublimation heat flux as a <see cref="HeatFlux"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override HeatFlux GetSurfaceHeatFluxesError(
        in Temperature surfaceTemperature,
        in CombustionSolverParams solverParams,
        ProblemContextByUnits context)
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
        contextBag.SublimationHeatFlux = HeatFlux.FromWattsPerSquareMeter(
            contextBag.DecomposeRate.KilogramsPerSecondPerSquareMeter
            * enthalpyChange.JoulesPerKilogram);

        return kineticContextBag.KineticFlameHeatFlux - contextBag.SublimationHeatFlux;
    }

    /// <summary>
    /// Extracts the kinetic burn parameters specific to the InterPocket model from the provided burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy for the kinetic flame.
    /// </summary>
    /// <param name="solverParams">
    /// A reference to the parameters related to the burn process, provided as a <see cref="CombustionSolverParams"/> object.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a <see cref="Frequency"/> object.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a <see cref="MolarEnergy"/> object.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void ExtractKineticBurnParams(
        in CombustionSolverParams solverParams,
        out Frequency aKineticFlame,
        out MolarEnergy eKineticFlame)
    {
        aKineticFlame = solverParams.AKineticFlameInterPocket;
        eKineticFlame = solverParams.EKineticFlameInterPocket;
    }

#endregion
}
