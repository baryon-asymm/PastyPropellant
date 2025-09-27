using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ComputedParams;

#region Utilization of Doubles

/// <summary>
/// Represents the parameters related to the kinetic flame combustion process, using native double values.
/// This struct includes properties such as flame height, density, temperature, and heat flux.
/// </summary>
public struct KineticFlameCombustionParamsByDoubles
{
    /// <summary>
    /// Gets or sets the height of the kinetic flame.
    /// Measured in meters (m).
    /// </summary>
    public double KineticFlameHeight;

    /// <summary>
    /// Gets or sets the average density of the kinetic flame.
    /// Measured in kilograms per cubic meter (kg/m³).
    /// </summary>
    public double AverageKineticFlameDensity;

    /// <summary>
    /// Gets or sets the average temperature of the kinetic flame.
    /// Measured in Kelvin (K).
    /// </summary>
    public double AverageKineticFlameTemperature;

    /// <summary>
    /// Gets or sets the heat flux of the kinetic flame.
    /// Measured in watts per square meter (W/m²).
    /// </summary>
    public double KineticFlameHeatFlux;
}

#endregion

#region Utilization of UnitsNet

/// <summary>
/// Represents the parameters related to the kinetic flame combustion process, using UnitsNet types.
/// This struct includes properties such as flame height, density, temperature, and heat flux.
/// </summary>
public struct KineticFlameCombustionParams
{
    /// <summary>
    /// Gets or sets the height of the kinetic flame.
    /// Measured using <see cref="UnitsNet.Length"/> in meters (m).
    /// </summary>
    public Length KineticFlameHeight;

    /// <summary>
    /// Gets or sets the average density of the kinetic flame.
    /// Measured using <see cref="UnitsNet.Density"/> in kilograms per cubic meter (kg/m³).
    /// </summary>
    public Density AverageKineticFlameDensity;

    /// <summary>
    /// Gets or sets the average temperature of the kinetic flame.
    /// Measured using <see cref="UnitsNet.Temperature"/> in Kelvin (K).
    /// </summary>
    public Temperature AverageKineticFlameTemperature;

    /// <summary>
    /// Gets or sets the heat flux of the kinetic flame.
    /// Measured using <see cref="UnitsNet.HeatFlux"/> in watts per square meter (W/m²).
    /// </summary>
    public HeatFlux KineticFlameHeatFlux;
}

#endregion
