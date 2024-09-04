using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

/// <summary>
/// Represents a set of thermodynamic parameters for modeling the behavior of metals in combustion processes.
/// This struct includes properties such as the melting and boiling temperatures of the metal.
/// These parameters are essential for understanding the thermal characteristics of metals involved in combustion processes.
/// </summary>
public readonly struct MetalCombustionParams
{
#region Properties

    /// <summary>
    /// Gets the melting temperature of the metal.
    /// This property indicates the temperature at which the metal transitions from a solid to a liquid state.
    /// It is crucial for modeling the thermal effects on the metal during combustion processes.
    /// </summary>
    public required Temperature MetalMeltingTemperature { get; init; }

    /// <summary>
    /// Gets the boiling temperature of the metal.
    /// This property indicates the temperature at which the metal transitions from a liquid to a gaseous state.
    /// It is important for understanding the metal's behavior at high temperatures and its potential vaporization during combustion.
    /// </summary>
    public required Temperature MetalBoilingTemperature { get; init; }

#endregion
}
