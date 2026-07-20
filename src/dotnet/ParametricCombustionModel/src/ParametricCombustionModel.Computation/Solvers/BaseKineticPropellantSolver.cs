using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Common;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Solvers;

/// <summary>
/// Represents an abstract base class for solving kinetic propellant combustion problems.
/// This class provides methods for calculating various parameters related to the kinetic flame,
/// such as heat flux, average temperature, density, and flame height.
/// </summary>
/// <remarks>
/// Derived classes must implement the abstract methods <see cref="ExtractKineticBurnParams(CombustionSolverParamsByUnits, out Frequency, out MolarEnergy)"/>
/// and <see cref="ExtractKineticBurnParams(CombustionSolverParamsByDoubles, out double, out double)"/>
/// to provide specific logic for extracting kinetic flame parameters.
/// </remarks>
public abstract class BaseKineticPropellantSolver : BasePropellantSolver
{
#region Abstract Methods

    /// <summary>
    /// Extracts the kinetic flame parameters from the given burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy
    /// specific to the kinetic flame from the provided burn parameters.
    /// </summary>
    /// <param name="solverParamsByUnits">
    /// A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByUnits"/>.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a <see cref="Frequency"/>.
    /// This parameter is used to characterize the reaction rate of the flame.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a <see cref="MolarEnergy"/>.
    /// This parameter represents the minimum energy required to initiate the flame reaction.
    /// </param>
    /// <remarks>
    /// This method is abstract and must be implemented by derived classes to provide
    /// specific logic for extracting kinetic flame parameters, which are essential
    /// for accurate modeling of flame dynamics in combustion analysis.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected abstract void ExtractKineticBurnParams(
        in CombustionSolverParamsByUnits solverParamsByUnits,
        out Frequency aKineticFlame,
        out MolarEnergy eKineticFlame,
        out double nu);

    /// <summary>
    /// Extracts the kinetic flame parameters from the given burn parameters using native double types.
    /// This method retrieves the pre-exponential factor and activation energy
    /// specific to the kinetic flame from the provided burn parameters.
    /// </summary>
    /// <param name="solverParams">
    /// A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByDoubles"/>.
    /// </param>
    /// <param name="aKineticFlame">
    /// The pre-exponential factor for the kinetic flame, returned as a double.
    /// This parameter is used to characterize the reaction rate of the flame.
    /// </param>
    /// <param name="eKineticFlame">
    /// The activation energy for the kinetic flame, returned as a double.
    /// This parameter represents the minimum energy required to initiate the flame reaction.
    /// </param>
    /// <remarks>
    /// This method is abstract and must be implemented by derived classes to provide
    /// specific logic for extracting kinetic flame parameters using double types, which are
    /// essential for accurate modeling of flame dynamics in combustion analysis.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected abstract void ExtractKineticBurnParams(
        in CombustionSolverParamsByDoubles solverParams,
        out double aKineticFlame,
        out double eKineticFlame,
        out double nu);

#endregion

#region Computation Methods

    /// <summary>
    /// Calculates the heat flux from the kinetic flame to the propellant surface 
    /// based on the given pressure, surface temperature, decomposition rate, burn parameters, 
    /// and kinetic flame parameters. This method models the heat transfer from the kinetic flame to the propellant surface 
    /// by considering thermal conductivity, flame height, and temperature differences.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber, expressed as a <see cref="Pressure"/>.</param>
    /// <param name="surfaceTemperature">The surface temperature of the propellant, expressed as a <see cref="Temperature"/>.</param>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes, expressed as a <see cref="MassFlux"/>.</param>
    /// <param name="solverParamsByUnits">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByUnits"/>.</param>
    /// <param name="kineticFlameParamsByUnits">A reference to the parameters related to the kinetic flame, provided as <see cref="KineticFlameParamsByUnits"/>.</param>
    /// <param name="contextBag">A reference to the <see cref="KineticFlameCombustionParams"/> context for storing intermediate computation results.</param>
    /// <returns>
    /// The heat flux from the kinetic flame to the propellant surface, represented as a <see cref="HeatFlux"/>.
    /// </returns>
    /// <remarks>
    /// The kinetic burn parameters are resolved through <see cref="ExtractKineticBurnParams(in CombustionSolverParamsByUnits, out Frequency, out MolarEnergy, out double)"/>
    /// and the flame math itself is delegated to <see cref="KineticFlameCalculator"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual HeatFlux GetKineticFlameHeatFlux(
        in Pressure pressure,
        in Temperature surfaceTemperature,
        in MassFlux decomposeRate,
        in CombustionSolverParamsByUnits solverParamsByUnits,
        in KineticFlameParamsByUnits kineticFlameParamsByUnits,
        ref KineticFlameCombustionParams contextBag)
    {
        ExtractKineticBurnParams(solverParamsByUnits, out var aKineticFlame, out var eKineticFlame, out var nu);

        return KineticFlameCalculator.GetKineticFlameHeatFlux(pressure,
                                                              surfaceTemperature,
                                                              decomposeRate,
                                                              aKineticFlame,
                                                              eKineticFlame,
                                                              nu,
                                                              kineticFlameParamsByUnits,
                                                              ref contextBag);
    }

#endregion

#region Computation Methods with Double Parameters

    /// <summary>
    /// Calculates the heat flux from the kinetic flame to the propellant surface 
    /// based on the given pressure, surface temperature, decomposition rate, burn parameters, 
    /// and kinetic flame parameters. This method models the heat transfer from the kinetic flame to the propellant surface 
    /// by considering thermal conductivity, flame height, and temperature differences.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber, expressed as a double.</param>
    /// <param name="surfaceTemperature">The surface temperature of the propellant, expressed as a double.</param>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes, expressed as a double.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <param name="kineticFlameParams">A reference to the parameters related to the kinetic flame, provided as <see cref="KineticFlameParamsByDoubles"/>.</param>
    /// <param name="contextBag">A reference to the <see cref="KineticFlameCombustionParamsByDoubles"/> context for storing intermediate computation results.</param>
    /// <returns>
    /// The heat flux from the kinetic flame to the propellant surface, represented as a double.
    /// </returns>
    /// <remarks>
    /// The kinetic burn parameters are resolved through <see cref="ExtractKineticBurnParams(in CombustionSolverParamsByDoubles, out double, out double, out double)"/>
    /// and the flame math itself is delegated to <see cref="KineticFlameCalculator"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual double GetKineticFlameHeatFlux(
        double pressure,
        double surfaceTemperature,
        double decomposeRate,
        in CombustionSolverParamsByDoubles solverParams,
        in KineticFlameParamsByDoubles kineticFlameParams,
        ref KineticFlameCombustionParamsByDoubles contextBag)
    {
        ExtractKineticBurnParams(solverParams, out var aKineticFlame, out var eKineticFlame, out var nu);

        return KineticFlameCalculator.GetKineticFlameHeatFlux(pressure,
                                                              surfaceTemperature,
                                                              decomposeRate,
                                                              aKineticFlame,
                                                              eKineticFlame,
                                                              nu,
                                                              kineticFlameParams,
                                                              ref contextBag);
    }

#endregion
}
