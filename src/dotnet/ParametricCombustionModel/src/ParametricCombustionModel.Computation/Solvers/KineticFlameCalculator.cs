using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Common;
using ParametricCombustionModel.Computation.Models.ComputedParams;
using ParametricCombustionModel.Computation.Models.KnownParams;
using UnitsNet;

namespace ParametricCombustionModel.Computation.Solvers;

/// <summary>
/// Stateless kinetic-flame math shared by the kinetic solvers (<see cref="BaseKineticPropellantSolver"/>)
/// and by the composed pocket-region helpers (<see cref="KineticSkeletonHelper"/>,
/// <see cref="KineticOutSkeletonHelper"/>).
/// </summary>
/// <remarks>
/// The kinetic pre-exponential factor, activation energy and density exponent are supplied by the caller
/// rather than resolved through a virtual hook, so that consumers which only need the flame math
/// do not have to inherit the solver (visitor) contract.
/// </remarks>
public static class KineticFlameCalculator
{
#region Computation Methods

    /// <summary>
    /// Calculates the heat flux from the kinetic flame to the propellant surface
    /// based on the given pressure, surface temperature, decomposition rate, kinetic burn parameters,
    /// and kinetic flame parameters. This method models the heat transfer from the kinetic flame to the propellant surface
    /// by considering thermal conductivity, flame height, and temperature differences.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber, expressed as a <see cref="Pressure"/>.</param>
    /// <param name="surfaceTemperature">The surface temperature of the propellant, expressed as a <see cref="Temperature"/>.</param>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes, expressed as a <see cref="MassFlux"/>.</param>
    /// <param name="aKineticFlame">The pre-exponential factor for the kinetic flame, expressed as a <see cref="Frequency"/>.</param>
    /// <param name="eKineticFlame">The activation energy for the kinetic flame, expressed as a <see cref="MolarEnergy"/>.</param>
    /// <param name="nu">The density exponent of the kinetic flame reaction rate.</param>
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
    public static HeatFlux GetKineticFlameHeatFlux(
        in Pressure pressure,
        in Temperature surfaceTemperature,
        in MassFlux decomposeRate,
        in Frequency aKineticFlame,
        in MolarEnergy eKineticFlame,
        double nu,
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
                                  aKineticFlame,
                                  eKineticFlame,
                                  nu);

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
    public static Temperature GetAverageKineticFlameTemperature(
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
    public static Density GetAverageKineticFlameDensity(
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
    /// average kinetic flame temperature, average kinetic flame density, and kinetic burn parameters.
    /// This method determines the height of the kinetic flame by taking into account
    /// the mass flux of decomposing propellant, the temperature and density of the flame,
    /// as well as specific kinetic parameters related to the burn process.
    /// </summary>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes, expressed as a <see cref="MassFlux"/>.</param>
    /// <param name="averageKineticFlameTemperature">The average temperature of the kinetic flame, expressed as a <see cref="Temperature"/>.</param>
    /// <param name="averageKineticFlameDensity">The average density of the kinetic flame, expressed as a <see cref="Density"/>.</param>
    /// <param name="aKineticFlame">The pre-exponential factor for the kinetic flame, expressed as a <see cref="Frequency"/>.</param>
    /// <param name="eKineticFlame">The activation energy for the kinetic flame, expressed as a <see cref="MolarEnergy"/>.</param>
    /// <param name="nu">The density exponent of the kinetic flame reaction rate.</param>
    /// <returns>
    /// The height of the kinetic flame, calculated as a <see cref="Length"/>.
    /// </returns>
    /// <remarks>
    /// This method calculates the flame height by utilizing kinetic flame parameters such as pre-exponential factors and activation energies,
    /// alongside the universal gas constant and the ideal gas law. The flame height is crucial for understanding the thermal exchange
    /// and combustion dynamics within the propellant burn process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Length GetKineticFlameHeight(
        in MassFlux decomposeRate,
        in Temperature averageKineticFlameTemperature,
        in Density averageKineticFlameDensity,
        in Frequency aKineticFlame,
        in MolarEnergy eKineticFlame,
        double nu)
    {
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
    /// based on the given pressure, surface temperature, decomposition rate, kinetic burn parameters,
    /// and kinetic flame parameters. This method models the heat transfer from the kinetic flame to the propellant surface
    /// by considering thermal conductivity, flame height, and temperature differences.
    /// </summary>
    /// <param name="pressure">The pressure in the rocket engine combustion chamber, expressed as a double.</param>
    /// <param name="surfaceTemperature">The surface temperature of the propellant, expressed as a double.</param>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes, expressed as a double.</param>
    /// <param name="aKineticFlame">The pre-exponential factor for the kinetic flame, expressed as a double.</param>
    /// <param name="eKineticFlame">The activation energy for the kinetic flame, expressed as a double.</param>
    /// <param name="nu">The density exponent of the kinetic flame reaction rate.</param>
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
    public static double GetKineticFlameHeatFlux(
        double pressure,
        double surfaceTemperature,
        double decomposeRate,
        double aKineticFlame,
        double eKineticFlame,
        double nu,
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
                                  aKineticFlame,
                                  eKineticFlame,
                                  nu);

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
    public static double GetAverageKineticFlameTemperature(
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
    public static double GetAverageKineticFlameDensity(
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
    /// average kinetic flame temperature, average kinetic flame density, and kinetic burn parameters.
    /// This method determines the height of the kinetic flame by taking into account
    /// the mass flux of decomposing propellant, the temperature and density of the flame,
    /// as well as specific kinetic parameters related to the burn process.
    /// </summary>
    /// <param name="decomposeRate">The mass flux rate at which the propellant decomposes, expressed as a double.</param>
    /// <param name="averageKineticFlameTemperature">The average temperature of the kinetic flame, expressed as a double.</param>
    /// <param name="averageKineticFlameDensity">The average density of the kinetic flame, expressed as a double.</param>
    /// <param name="aKineticFlame">The pre-exponential factor for the kinetic flame, expressed as a double.</param>
    /// <param name="eKineticFlame">The activation energy for the kinetic flame, expressed as a double.</param>
    /// <param name="nu">The density exponent of the kinetic flame reaction rate.</param>
    /// <returns>
    /// The height of the kinetic flame, calculated as a double.
    /// </returns>
    /// <remarks>
    /// This method calculates the flame height by utilizing kinetic flame parameters such as pre-exponential factors and activation energies,
    /// alongside the universal gas constant and the ideal gas law. The flame height is crucial for understanding the thermal exchange
    /// and combustion dynamics within the propellant burn process.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static double GetKineticFlameHeight(
        in double decomposeRate,
        in double averageKineticFlameTemperature,
        in double averageKineticFlameDensity,
        double aKineticFlame,
        double eKineticFlame,
        double nu)
    {
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
