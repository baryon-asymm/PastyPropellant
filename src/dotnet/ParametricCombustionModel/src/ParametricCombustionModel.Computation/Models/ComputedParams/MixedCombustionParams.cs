using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.ComputedParams;

#region Utilization of Doubles

/// <summary>
/// Represents the parameters related to the mixed combustion process, using native double values.
/// This struct includes properties related to the burn rate of the material.
/// </summary>
public struct MixedCombustionParamsByDoubles
{
    /// <summary>
    /// Indicates whether the burn rate has been successfully found.
    /// </summary>
    public bool BurnRateIsFound;

    /// <summary>
    /// Gets or sets the burn rate of the material.
    /// Measured in meters per second (m/s).
    /// </summary>
    public double BurnRate;
}

#endregion

#region Utilization of UnitsNet

/// <summary>
/// Represents the parameters related to the mixed combustion process, using UnitsNet types.
/// This struct includes properties related to the burn rate of the material.
/// </summary>
public struct MixedCombustionParams
{
    /// <summary>
    /// Indicates whether the burn rate has been successfully found.
    /// </summary>
    public bool BurnRateIsFound;

    /// <summary>
    /// Gets or sets the burn rate of the material.
    /// Measured using <see cref="UnitsNet.Speed"/> in meters per second (m/s).
    /// </summary>
    public Speed BurnRate;
}

#endregion
