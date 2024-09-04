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
/// Derived classes must implement the abstract method <see cref="ExtractKineticBurnParams"/>
/// to provide specific logic for extracting kinetic flame parameters.
/// </remarks>
public abstract class BaseKineticPropellantSolver : BasePropellantSolver
{
#region Abstracts

    /// <summary>
    /// Extracts the kinetic flame parameters from the given burn parameters.
    /// This method retrieves the pre-exponential factor and activation energy
    /// specific to the kinetic flame from the provided burn parameters.
    /// </summary>
    /// <param name="solverParams">
    /// A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParams"/>.
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void ExtractKineticBurnParams(
        in CombustionSolverParams solverParams,
        out Frequency aKineticFlame,
        out MolarEnergy eKineticFlame);

#endregion

#region Computation Methods

    /// <summary>
    /// Calculates the heat flux from the kinetic flame to the propellant surface 
    /// based on the given pressure, surface temperature, decomposition rate, burn parameters, 
    /// and kinetic flame parameters. This method models the heat transfer from the kinetic flame to the propellant surface 
    /// by considering thermal conductivity, flame height, and temperature differences.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber.</param>
    /// <param name="surfaceTemperature">The surface temperature of the propellant (temperature of the condensed phase).</param>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, such as reaction rates and activation energies.</param>
    /// <param name="kineticFlameParams">A reference to the parameters related to the kinetic flame, such as thermal conductivity and final flame temperature.</param>
    /// <returns>
    /// The heat flux from the kinetic flame to the propellant surface, represented as a <see cref="HeatFlux"/>.
    /// </returns>
    /// <remarks>
    /// This method uses the average kinetic flame temperature and density, along with the flame height, to calculate the heat flux. 
    /// It applies Fourier's law of heat conduction and takes into account the temperature gradient between the flame and the propellant surface.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual HeatFlux GetKineticFlameHeatFlux(
        in Pressure pressure,
        in Temperature surfaceTemperature,
        in MassFlux decomposeRate,
        in CombustionSolverParams solverParams,
        in KineticFlameParams kineticFlameParams,
        ref KineticFlameCombustionParams contextBag)
    {
        var thermalConductivity = kineticFlameParams.ThermalConductivity;
        var kineticFlameTemperature = kineticFlameParams.FinalTemperature;

        contextBag.AverageKineticFlameTemperature =
            GetAverageKineticFlameTemperature(surfaceTemperature,
                                              kineticFlameParams);
        contextBag.AverageKineticFlameDensity =
            GetAverageKineticFlameDensity(pressure,
                                          contextBag.AverageKineticFlameTemperature,
                                          kineticFlameParams);
        contextBag.KineticFlameHeight =
            GetKineticFlameHeight(decomposeRate,
                                  contextBag.AverageKineticFlameTemperature,
                                  contextBag.AverageKineticFlameDensity,
                                  solverParams);

        var heatFluxDouble = thermalConductivity.WattsPerMeterKelvin
                             * (kineticFlameTemperature - surfaceTemperature).Kelvins
                             / contextBag.KineticFlameHeight.Meters;

        var heatFlux = HeatFlux.FromWattsPerSquareMeter(heatFluxDouble);

        return heatFlux;
    }

    /// <summary>
    /// Calculates the average kinetic flame temperature by averaging the final kinetic flame temperature
    /// and the surface temperature of the propellant. This method provides an estimate of the mean temperature
    /// of the kinetic flame layer, which is useful for further thermal analysis and heat flux calculations.
    /// </summary>
    /// <param name="surfaceTemperature">The surface temperature of the propellant (temperature of the condensed phase).</param>
    /// <param name="kineticFlameParams">A reference to the parameters related to the kinetic flame, including properties such as the final flame temperature.</param>
    /// <returns>
    /// The average kinetic flame temperature as a <see cref="Temperature"/>.
    /// </returns>
    /// <remarks>
    /// This method is primarily used in the calculation of the heat flux from the kinetic flame to the propellant surface,
    /// providing a simplified mean temperature of the flame that contributes to the thermal exchange processes.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual Temperature GetAverageKineticFlameTemperature(
        in Temperature surfaceTemperature,
        in KineticFlameParams kineticFlameParams)
    {
        var kineticFlameTemperature = kineticFlameParams.FinalTemperature;

        var averageKineticFlameTemperatureDouble =
            (kineticFlameTemperature.Kelvins + surfaceTemperature.Kelvins) / 2.0;
        var averageKineticFlameTemperature = Temperature.FromKelvins(averageKineticFlameTemperatureDouble);

        return averageKineticFlameTemperature;
    }

    /// <summary>
    /// Calculates the average kinetic flame density using the ideal gas law, based on the given pressure,
    /// average kinetic flame temperature, and kinetic flame parameters. This method provides an estimate
    /// of the mean density of the kinetic flame, which is essential for thermal and combustion analysis.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber, expressed as a <see cref="Pressure"/>.</param>
    /// <param name="averageKineticFlameTemperature">The average temperature of the kinetic flame, expressed as a <see cref="Temperature"/>.</param>
    /// <param name="kineticFlameParams">A reference to the parameters related to the kinetic flame, such as the average molar mass of the flame components.</param>
    /// <returns>
    /// The average kinetic flame density as a <see cref="Density"/>.
    /// </returns>
    /// <remarks>
    /// This method is instrumental in calculating the kinetic flame height and heat flux, by providing an estimate
    /// of the average density of the flame. The density is a key factor in determining how heat is transferred
    /// within the combustion environment, affecting the overall efficiency and behavior of the combustion process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual Density GetAverageKineticFlameDensity(
        in Pressure pressure,
        in Temperature averageKineticFlameTemperature,
        in KineticFlameParams kineticFlameParams)
    {
        var averageMolarMass = kineticFlameParams.AverageMolarMass;
        const double gasConstant = PhysicalConstants.UniversalGasConstant; // J/(mol*K)

        var averageDensityDouble = pressure.Pascals
                                   * averageMolarMass.KilogramsPerMole
                                   / (gasConstant * averageKineticFlameTemperature.Kelvins);
        var averageDensity = Density.FromKilogramsPerCubicMeter(averageDensityDouble);

        return averageDensity;
    }

    /// <summary>
    /// Calculates the kinetic flame height based on the given decomposition rate, 
    /// average kinetic flame temperature, average kinetic flame density, and burn parameters.
    /// This method determines the height of the kinetic flame by taking into account
    /// the mass flux of decomposing propellant, the temperature and density of the flame,
    /// as well as specific kinetic parameters related to the burn process.
    /// </summary>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes, expressed as a <see cref="MassFlux"/>.</param>
    /// <param name="averageKineticFlameTemperature">The average temperature of the kinetic flame, expressed as a <see cref="Temperature"/>.</param>
    /// <param name="averageKineticFlameDensity">The average density of the kinetic flame, expressed as a <see cref="Density"/>.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, including reaction rates and activation energies, provided as <see cref="CombustionSolverParams"/>.</param>
    /// <returns>
    /// The height of the kinetic flame, calculated as a <see cref="Length"/>.
    /// </returns>
    /// <remarks>
    /// This method calculates the flame height by utilizing kinetic flame parameters such as pre-exponential factors and activation energies, 
    /// alongside the universal gas constant and the ideal gas law. The flame height is crucial for understanding the thermal exchange 
    /// and combustion dynamics within the propellant burn process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual Length GetKineticFlameHeight(
        in MassFlux decomposeRate,
        in Temperature averageKineticFlameTemperature,
        in Density averageKineticFlameDensity,
        in CombustionSolverParams solverParams)
    {
        ExtractKineticBurnParams(solverParams, out var aKineticFlame, out var eKineticFlame);
        var nu = solverParams.Nu;
        const double gasConstant = PhysicalConstants.UniversalGasConstant; // J/(mol*K)

        var molarEnergy = MolarEnergy.FromJoulesPerMole(gasConstant * averageKineticFlameTemperature.Kelvins);
        var poweredDensityDouble = Math.Pow(averageKineticFlameDensity.KilogramsPerCubicMeter, nu);
        var volumedMassFlowDouble = aKineticFlame.PerSecond * poweredDensityDouble; // kg/(m^3*s)
        var flameHeightDouble = decomposeRate.KilogramsPerSecondPerSquareMeter
                                / (
                                      volumedMassFlowDouble
                                      * Math.Exp(-eKineticFlame / molarEnergy)
                                  );
        var flameHeight = Length.FromMeters(flameHeightDouble);

        return flameHeight;
    }

#endregion
}
