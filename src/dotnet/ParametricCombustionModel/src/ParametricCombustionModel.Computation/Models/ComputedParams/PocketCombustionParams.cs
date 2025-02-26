using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ComputedParams;

#region Utilization of Doubles

/// <summary>
/// Represents the parameters related to the pocket combustion process, using native double values.
/// This struct includes properties such as burn rate, surface temperature, decomposition rate, and various heat fluxes.
/// </summary>
public struct PocketCombustionParamsByDoubles
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
    /// Gets or sets the kinetic flame combustion parameters for the out-skeleton process.
    /// </summary>
    public KineticFlameCombustionParamsByDoubles OutSkeletonKineticFlameCombustionParams;

    /// <summary>
    /// Gets or sets the kinetic flame combustion parameters for the skeleton process.
    /// </summary>
    public KineticFlameCombustionParamsByDoubles SkeletonKineticFlameCombustionParams;

#region Inside Skeleton Metal Burning Parameters

    /// <summary>
    /// Gets or sets the average temperature of metal burning.
    /// Measured in Kelvin (K).
    /// </summary>
    public double AverageMetalBurningTemperature;

    /// <summary>
    /// Gets or sets the heat flux associated with metal burning.
    /// Measured in watts per square meter (W/m²).
    /// </summary>
    public double MetalBurningHeatFlux;

#endregion

#region Diffusion Flame Parameters

    /// <summary>
    /// Gets or sets the height of the diffusion flame.
    /// Measured in meters (m).
    /// </summary>
    public double DiffusionFlameHeight;

    /// <summary>
    /// Gets or sets the heat flux of the diffusion flame.
    /// Measured in watts per square meter (W/m²).
    /// </summary>
    public double DiffusionFlameHeatFlux;

#endregion

    /// <summary>
    /// Gets or sets the heat flux associated with the out-skeleton process.
    /// Measured in watts per square meter (W/m²).
    /// </summary>
    public double OutSkeletonHeatFlux;

    /// <summary>
    /// Gets or sets the heat flux associated with the skeleton process.
    /// Measured in watts per square meter (W/m²).
    /// </summary>
    public double SkeletonHeatFlux;

    /// <summary>
    /// Gets or sets the total heat flux to the surface.
    /// Measured in watts per square meter (W/m²).
    /// </summary>
    public double ToSurfaceTotalHeatFlux;

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
/// Represents the parameters related to the pocket combustion process, using UnitsNet types.
/// This struct includes properties such as burn rate, surface temperature, decomposition rate, and various heat fluxes.
/// </summary>
public struct PocketCombustionParams
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
    /// Gets or sets the kinetic flame combustion parameters for the out-skeleton process.
    /// </summary>
    public KineticFlameCombustionParams OutSkeletonKineticFlameCombustionParams;

    /// <summary>
    /// Gets or sets the kinetic flame combustion parameters for the skeleton process.
    /// </summary>
    public KineticFlameCombustionParams SkeletonKineticFlameCombustionParams;

#region Inside Skeleton Metal Burning Parameters

    /// <summary>
    /// Gets or sets the average temperature of metal burning.
    /// Measured using <see cref="UnitsNet.Temperature"/> in Kelvin (K).
    /// </summary>
    public Temperature AverageMetalBurningTemperature;

    /// <summary>
    /// Gets or sets the heat flux associated with metal burning.
    /// Measured using <see cref="UnitsNet.HeatFlux"/> in watts per square meter (W/m²).
    /// </summary>
    public HeatFlux MetalBurningHeatFlux;

#endregion

#region Diffusion Flame Parameters

    /// <summary>
    /// Gets or sets the height of the diffusion flame.
    /// Measured using <see cref="UnitsNet.Length"/> in meters (m).
    /// </summary>
    public Length DiffusionFlameHeight;

    /// <summary>
    /// Gets or sets the heat flux of the diffusion flame.
    /// Measured using <see cref="UnitsNet.HeatFlux"/> in watts per square meter (W/m²).
    /// </summary>
    public HeatFlux DiffusionFlameHeatFlux;

#endregion

    /// <summary>
    /// Gets or sets the heat flux associated with the out-skeleton process.
    /// Measured using <see cref="UnitsNet.HeatFlux"/> in watts per square meter (W/m²).
    /// </summary>
    public HeatFlux OutSkeletonHeatFlux;

    /// <summary>
    /// Gets or sets the heat flux associated with the skeleton process.
    /// Measured using <see cref="UnitsNet.HeatFlux"/> in watts per square meter (W/m²).
    /// </summary>
    public HeatFlux SkeletonHeatFlux;

    /// <summary>
    /// Gets or sets the total heat flux to the surface.
    /// Measured using <see cref="UnitsNet.HeatFlux"/> in watts per square meter (W/m²).
    /// </summary>
    public HeatFlux ToSurfaceTotalHeatFlux;

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
