using ParametricCombustionModel.Core.Models;
using UnitsNet;

namespace ParametricCombustionModel.Optimization.Extensions;

/// <summary>
/// Provides extension methods for calculating the experimental burning rates of propellants.
/// These methods allow you to calculate burning rates based on the physical properties of propellants
/// and varying pressures, supporting the use of <see cref="UnitsNet.Pressure"/> for more precise and unit-aware calculations.
/// </summary>
public static class PropellantExtensions
{
    /// <summary>
    /// Calculates the experimental burning rate of a propellant based on the given pressure.
    /// The burning rate is computed using the formula A * pressure^Nu, where A and Nu are propellant-specific parameters.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the burning rate is calculated. 
    /// It contains the material-specific parameters A (rate constant) and Nu (pressure exponent) that are used in the calculation.
    /// </param>
    /// <param name="pressure">
    /// The pressure in Pascals at which the burning rate is evaluated.
    /// </param>
    /// <returns>
    /// The experimental burning rate for the given propellant at the specified pressure.
    /// </returns>
    public static double GetExperimentalBurnRate(
        this Propellant propellant,
        double pressure)
    {
        return propellant.A * Math.Pow(pressure, propellant.Nu);
    }

    /// <summary>
    /// Calculates the experimental burning rate of a propellant based on the given pressure.
    /// The burning rate is computed using the formula A * pressure^Nu, where A and Nu are propellant-specific parameters.
    /// </summary>
    /// <param name="propellant">
    /// The <see cref="Propellant"/> instance for which the burning rate is calculated.
    /// It contains the material-specific parameters A (rate constant) and Nu (pressure exponent) that are used in the calculation.
    /// </param>
    /// <param name="pressure">
    /// The pressure at which the burning rate is evaluated, represented by a <see cref="UnitsNet.Pressure"/> value in Pascals.
    /// </param>
    /// <returns>
    /// The experimental burning rate for the given propellant at the specified pressure.
    /// </returns>
    public static Speed GetExperimentalBurnRate(
        this Propellant propellant,
        Pressure pressure)
    {
        return Speed.FromMetersPerSecond(
            propellant.A * Math.Pow(pressure.Pascals, propellant.Nu));
    }
}
