using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

#region Utilization of Doubles

/// <summary>
/// Represents a set of thermodynamic parameters for modeling the behavior of metals in combustion processes using native double values.
/// This struct includes properties such as the melting and boiling temperatures of the metal.
/// These parameters are essential for understanding the thermal characteristics of metals involved in combustion processes.
/// </summary>
public readonly struct MetalCombustionParamsByDoubles
{
    /// <summary>
    /// Gets the melting temperature of the metal.
    /// This property indicates the temperature at which the metal transitions from a solid to a liquid state.
    /// It is crucial for modeling the thermal effects on the metal during combustion processes.
    /// Measured in Kelvin (K).
    /// </summary>
    public required double MetalMeltingTemperature { get; init; }

    /// <summary>
    /// Gets the boiling temperature of the metal.
    /// This property indicates the temperature at which the metal transitions from a liquid to a gaseous state.
    /// It is important for understanding the metal's behavior at high temperatures and its potential vaporization during combustion.
    /// Measured in Kelvin (K).
    /// </summary>
    public required double MetalBoilingTemperature { get; init; }
}

#endregion

#region Utilization of UnitsNet

/// <summary>
/// Represents a set of thermodynamic parameters for modeling the behavior of metals in combustion processes.
/// This struct includes properties such as the melting and boiling temperatures of the metal.
/// These parameters are essential for understanding the thermal characteristics of metals involved in combustion processes.
/// </summary>
public struct MetalCombustionParamsByUnits
{
    /// <summary>
    /// Gets the melting temperature of the metal.
    /// This property indicates the temperature at which the metal transitions from a solid to a liquid state.
    /// It is crucial for modeling the thermal effects on the metal during combustion processes.
    /// </summary>
    public required Temperature MetalMeltingTemperature { get; set; }

    /// <summary>
    /// Gets the boiling temperature of the metal.
    /// This property indicates the temperature at which the metal transitions from a liquid to a gaseous state.
    /// It is important for understanding the metal's behavior at high temperatures and its potential vaporization during combustion.
    /// </summary>
    public required Temperature MetalBoilingTemperature { get; init; }
}

#endregion
