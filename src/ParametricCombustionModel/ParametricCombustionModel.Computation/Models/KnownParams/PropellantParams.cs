using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

#region Utilization of Doubles

/// <summary>
/// Represents a set of parameters related to the propellant used in combustion modeling using native double values.
/// This struct includes properties such as specific heat capacity, density, initial temperature, average oxidizer diameter, and surface fraction.
/// These parameters are essential for accurately modeling the thermodynamic and physical behavior of the propellant during combustion.
/// </summary>
public readonly struct PropellantParamsByDoubles
{
    /// <summary>
    /// Gets the specific heat capacity of the propellant.
    /// This property represents the amount of heat required to raise the temperature of a unit mass of the propellant by one degree Kelvin.
    /// Measured in J/(kg*K).
    /// </summary>
    public required double SpecificHeatCapacity { get; init; }

    /// <summary>
    /// Gets the density of the propellant.
    /// This property indicates the mass of the propellant per unit volume.
    /// Measured in kg/m^3.
    /// </summary>
    public required double Density { get; init; }

    /// <summary>
    /// Gets the initial temperature of the propellant.
    /// This property represents the temperature of the propellant before the start of the combustion process.
    /// Measured in Kelvin (K).
    /// </summary>
    public required double InitialTemperature { get; init; }

    /// <summary>
    /// Gets the average diameter of the oxidizer particles in the propellant.
    /// This property is used to characterize the physical dimensions of the oxidizer particles.
    /// Measured in meters (m).
    /// </summary>
    public required double AverageOxidizerDiameter { get; init; }

    /// <summary>
    /// Gets the fraction of the propellant surface occupied by the pocket.
    /// This property represents the proportion of the surface area that is occupied by pockets in the propellant.
    /// It is a dimensionless value (ratio).
    /// </summary>
    public required double SkeletonSurfaceFraction { get; init; }
}

#endregion

#region Utilization of UnitsNet

/// <summary>
/// Represents a set of parameters related to the propellant used in combustion modeling.
/// This struct includes properties such as specific heat capacity, density, initial temperature, average oxidizer diameter, and surface fraction.
/// These parameters are essential for accurately modeling the thermodynamic and physical behavior of the propellant during combustion.
/// </summary>
public readonly struct PropellantParams
{
    /// <summary>
    /// Gets the specific heat capacity of the propellant.
    /// This property represents the amount of heat required to raise the temperature of a unit mass of the propellant by one degree Kelvin.
    /// It is crucial for calculating thermal effects and energy changes in the propellant during combustion.
    /// </summary>
    public required SpecificEntropy SpecificHeatCapacity { get; init; }

    /// <summary>
    /// Gets the density of the propellant.
    /// This property indicates the mass of the propellant per unit volume. It is used to determine the mass flow rate and other volumetric properties.
    /// </summary>
    public required Density Density { get; init; }

    /// <summary>
    /// Gets the initial temperature of the propellant.
    /// This property represents the temperature of the propellant before the start of the combustion process.
    /// It is used as a baseline for calculating temperature changes and thermal effects during combustion.
    /// </summary>
    public required Temperature InitialTemperature { get; init; }

    /// <summary>
    /// Gets the average diameter of the oxidizer particles in the propellant.
    /// This property is used to characterize the physical dimensions of the oxidizer particles, which can affect the combustion dynamics.
    /// </summary>
    public required Length AverageOxidizerDiameter { get; init; }

    /// <summary>
    /// Gets the fraction of the propellant surface occupied by the pocket.
    /// This property represents the proportion of the surface area that is occupied by pockets in the propellant.
    /// It is important for modeling the distribution of heat and combustion characteristics.
    /// </summary>
    public required Ratio SkeletonSurfaceFraction { get; init; }
}

#endregion
