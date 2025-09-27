using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ComputedParams;

#region Utilization of Doubles

/// <summary>
/// Represents the parameters related to the combustion process between pockets, using native double values.
/// This struct includes properties such as burn rate, surface temperature, decompose rate, and heat fluxes.
/// </summary>
public struct InterPocketCombustionParamsByDoubles
{
    /// <summary>
    /// Indicates whether the burn rate has been found.
    /// </summary>
    public bool BurnRateIsFound;

    /// <summary>
    /// Gets or sets the surface temperature during combustion.
    /// Measured in Kelvin (K).
    /// </summary>
    public double SurfaceTemperature;

    /// <summary>
    /// Gets or sets the burn rate of the material.
    /// Measured in meters per second (m/s).
    /// </summary>
    public double BurnRate;

    /// <summary>
    /// Gets or sets the decomposition rate of the material.
    /// Measured in kilograms per second per square meter (kg/(m²*s)).
    /// </summary>
    public double DecomposeRate;

    /// <summary>
    /// Gets or sets the kinetic flame combustion parameters related to this inter-pocket process.
    /// </summary>
    public KineticFlameCombustionParamsByDoubles KineticFlameCombustionParams;

    /// <summary>
    /// Gets or sets the heat flux due to sublimation.
    /// Measured in watts per square meter (W/m²).
    /// </summary>
    public double SublimationHeatFlux;

    /// <summary>
    /// Gets or sets the error in surface heat fluxes.
    /// Measured in watts per square meter (W/m²).
    /// </summary>
    public double SurfaceHeatFluxesError;
}

#endregion

#region Utilization of UnitsNet

/// <summary>
/// Represents the parameters related to the combustion process between pockets, using UnitsNet types.
/// This struct includes properties such as burn rate, surface temperature, decompose rate, and heat fluxes.
/// </summary>
public struct InterPocketCombustionParams
{
    /// <summary>
    /// Indicates whether the burn rate has been found.
    /// </summary>
    public bool BurnRateIsFound;

    /// <summary>
    /// Gets or sets the surface temperature during combustion.
    /// Measured using <see cref="UnitsNet.Temperature"/> in Kelvin (K).
    /// </summary>
    public Temperature SurfaceTemperature;

    /// <summary>
    /// Gets or sets the burn rate of the material.
    /// Measured using <see cref="UnitsNet.Speed"/> in meters per second (m/s).
    /// </summary>
    public Speed BurnRate;

    /// <summary>
    /// Gets or sets the decomposition rate of the material.
    /// Measured using <see cref="UnitsNet.MassFlux"/> in kilograms per second per square meter (kg/(m²*s)).
    /// </summary>
    public MassFlux DecomposeRate;

    /// <summary>
    /// Gets or sets the kinetic flame combustion parameters related to this inter-pocket process.
    /// </summary>
    public KineticFlameCombustionParams KineticFlameCombustionParams;

    /// <summary>
    /// Gets or sets the heat flux due to sublimation.
    /// Measured using <see cref="UnitsNet.HeatFlux"/> in watts per square meter (W/m²).
    /// </summary>
    public HeatFlux SublimationHeatFlux;

    /// <summary>
    /// Gets or sets the error in surface heat fluxes.
    /// Measured using <see cref="UnitsNet.HeatFlux"/> in watts per square meter (W/m²).
    /// </summary>
    public HeatFlux SurfaceHeatFluxesError;
}

#endregion
