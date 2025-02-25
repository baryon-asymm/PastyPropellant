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
    /// This method uses the average kinetic flame temperature and density, along with the flame height, to calculate the heat flux. 
    /// It applies Fourier's law of heat conduction and takes into account the temperature gradient between the flame and the propellant surface.
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
        var thermalConductivity = kineticFlameParamsByUnits.ThermalConductivity;
        var kineticFlameTemperature = kineticFlameParamsByUnits.FinalTemperature;

        contextBag.AverageKineticFlameTemperature =
            GetAverageKineticFlameTemperature(surfaceTemperature,
                                              kineticFlameParamsByUnits);
        contextBag.AverageKineticFlameDensity =
            GetAverageKineticFlameDensity(pressure,
                                          contextBag.AverageKineticFlameTemperature,
                                          kineticFlameParamsByUnits);
        contextBag.KineticFlameHeight =
            GetKineticFlameHeight(decomposeRate,
                                  contextBag.AverageKineticFlameTemperature,
                                  contextBag.AverageKineticFlameDensity,
                                  solverParamsByUnits);

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
    /// <param name="surfaceTemperature">The surface temperature of the propellant, expressed as a <see cref="Temperature"/>.</param>
    /// <param name="kineticFlameParamsByUnits">A reference to the parameters related to the kinetic flame, including properties such as the final flame temperature.</param>
    /// <returns>
    /// The average kinetic flame temperature as a <see cref="Temperature"/>.
    /// </returns>
    /// <remarks>
    /// This method is primarily used in the calculation of the heat flux from the kinetic flame to the propellant surface,
    /// providing a simplified mean temperature of the flame that contributes to the thermal exchange processes.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual Temperature GetAverageKineticFlameTemperature(
        in Temperature surfaceTemperature,
        in KineticFlameParamsByUnits kineticFlameParamsByUnits)
    {
        var kineticFlameTemperature = kineticFlameParamsByUnits.FinalTemperature;

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
    /// <param name="kineticFlameParamsByUnits">A reference to the parameters related to the kinetic flame, such as the average molar mass of the flame components.</param>
    /// <returns>
    /// The average kinetic flame density as a <see cref="Density"/>.
    /// </returns>
    /// <remarks>
    /// This method is instrumental in calculating the kinetic flame height and heat flux, by providing an estimate
    /// of the average density of the flame. The density is a key factor in determining how heat is transferred
    /// within the combustion environment, affecting the overall efficiency and behavior of the combustion process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual Density GetAverageKineticFlameDensity(
        in Pressure pressure,
        in Temperature averageKineticFlameTemperature,
        in KineticFlameParamsByUnits kineticFlameParamsByUnits)
    {
        var averageMolarMass = kineticFlameParamsByUnits.AverageMolarMass;
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
    /// <param name="solverParamsByUnits">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByUnits"/>.</param>
    /// <returns>
    /// The height of the kinetic flame, calculated as a <see cref="Length"/>.
    /// </returns>
    /// <remarks>
    /// This method calculates the flame height by utilizing kinetic flame parameters such as pre-exponential factors and activation energies, 
    /// alongside the universal gas constant and the ideal gas law. The flame height is crucial for understanding the thermal exchange 
    /// and combustion dynamics within the propellant burn process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual Length GetKineticFlameHeight(
        in MassFlux decomposeRate,
        in Temperature averageKineticFlameTemperature,
        in Density averageKineticFlameDensity,
        in CombustionSolverParamsByUnits solverParamsByUnits)
    {
        ExtractKineticBurnParams(solverParamsByUnits, out var aKineticFlame, out var eKineticFlame, out var nu);
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
    /// This method uses the average kinetic flame temperature and density, along with the flame height, to calculate the heat flux. 
    /// It applies Fourier's law of heat conduction and takes into account the temperature gradient between the flame and the propellant surface.
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

        var heatFlux = thermalConductivity
                       * (kineticFlameTemperature - surfaceTemperature)
                       / contextBag.KineticFlameHeight;

        return heatFlux;
    }

    /// <summary>
    /// Calculates the average kinetic flame temperature by averaging the final kinetic flame temperature
    /// and the surface temperature of the propellant. This method provides an estimate of the mean temperature
    /// of the kinetic flame layer, which is useful for further thermal analysis and heat flux calculations.
    /// </summary>
    /// <param name="surfaceTemperature">The surface temperature of the propellant, expressed as a double.</param>
    /// <param name="kineticFlameParams">A reference to the parameters related to the kinetic flame, including properties such as the final flame temperature.</param>
    /// <returns>
    /// The average kinetic flame temperature as a double.
    /// </returns>
    /// <remarks>
    /// This method is primarily used in the calculation of the heat flux from the kinetic flame to the propellant surface,
    /// providing a simplified mean temperature of the flame that contributes to the thermal exchange processes.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual double GetAverageKineticFlameTemperature(
        in double surfaceTemperature,
        in KineticFlameParamsByDoubles kineticFlameParams)
    {
        var kineticFlameTemperature = kineticFlameParams.FinalTemperature;

        var averageKineticFlameTemperature =
            (kineticFlameTemperature + surfaceTemperature) / 2.0;

        return averageKineticFlameTemperature;
    }

    /// <summary>
    /// Calculates the average kinetic flame density using the ideal gas law, based on the given pressure,
    /// average kinetic flame temperature, and kinetic flame parameters. This method provides an estimate
    /// of the mean density of the kinetic flame, which is essential for thermal and combustion analysis.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber, expressed as a double.</param>
    /// <param name="averageKineticFlameTemperature">The average temperature of the kinetic flame, expressed as a double.</param>
    /// <param name="kineticFlameParams">A reference to the parameters related to the kinetic flame, such as the average molar mass of the flame components.</param>
    /// <returns>
    /// The average kinetic flame density as a double.
    /// </returns>
    /// <remarks>
    /// This method is instrumental in calculating the kinetic flame height and heat flux, by providing an estimate
    /// of the average density of the flame. The density is a key factor in determining how heat is transferred
    /// within the combustion environment, affecting the overall efficiency and behavior of the combustion process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual double GetAverageKineticFlameDensity(
        in double pressure,
        in double averageKineticFlameTemperature,
        in KineticFlameParamsByDoubles kineticFlameParams)
    {
        var averageMolarMass = kineticFlameParams.AverageMolarMass;
        const double gasConstant = PhysicalConstants.UniversalGasConstant; // J/(mol*K)

        var averageDensity =
            pressure
            * averageMolarMass
            / (gasConstant * averageKineticFlameTemperature);

        return averageDensity;
    }

    /// <summary>
    /// Calculates the kinetic flame height based on the given decomposition rate, 
    /// average kinetic flame temperature, average kinetic flame density, and burn parameters.
    /// This method determines the height of the kinetic flame by taking into account
    /// the mass flux of decomposing propellant, the temperature and density of the flame,
    /// as well as specific kinetic parameters related to the burn process.
    /// </summary>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes, expressed as a double.</param>
    /// <param name="averageKineticFlameTemperature">The average temperature of the kinetic flame, expressed as a double.</param>
    /// <param name="averageKineticFlameDensity">The average density of the kinetic flame, expressed as a double.</param>
    /// <param name="solverParams">A reference to the parameters related to the burn process, provided as <see cref="CombustionSolverParamsByDoubles"/>.</param>
    /// <returns>
    /// The height of the kinetic flame, calculated as a double.
    /// </returns>
    /// <remarks>
    /// This method calculates the flame height by utilizing kinetic flame parameters such as pre-exponential factors and activation energies, 
    /// alongside the universal gas constant and the ideal gas law. The flame height is crucial for understanding the thermal exchange 
    /// and combustion dynamics within the propellant burn process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected virtual double GetKineticFlameHeight(
        in double decomposeRate,
        in double averageKineticFlameTemperature,
        in double averageKineticFlameDensity,
        in CombustionSolverParamsByDoubles solverParams)
    {
        ExtractKineticBurnParams(solverParams, out var aKineticFlame, out var eKineticFlame, out var nu);
        const double gasConstant = PhysicalConstants.UniversalGasConstant; // J/(mol*K)

        var molarEnergy = gasConstant * averageKineticFlameTemperature;
        var poweredDensity = Math.Pow(averageKineticFlameDensity, nu);
        var volumedMassFlow = aKineticFlame * poweredDensity; // kg/(m^3*s)
        var flameHeight = decomposeRate
                          / (
                                volumedMassFlow
                                * Math.Exp(-eKineticFlame / molarEnergy)
                            );

        return flameHeight;
    }

#endregion
}
