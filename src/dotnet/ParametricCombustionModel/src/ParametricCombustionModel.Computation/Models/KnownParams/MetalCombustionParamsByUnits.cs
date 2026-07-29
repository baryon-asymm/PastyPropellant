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

    /// <summary>
    /// Dimensionless divisor applied to the Fourier conduction flux through the skeleton layer
    /// (see <c>PocketPropellantSolver.GetMetalBurningHeatFlux</c>). Physically it is the reciprocal of the
    /// fraction of the skeleton footprint in genuine conductive contact with the surface: the metal skeleton
    /// touches the condensed phase at oxide-separated spots, not over its whole projected area, so the
    /// bulk Fourier law over-predicts the flux by the ratio of nominal to real contact area.
    ///
    /// <para><b>1.0 means no correction</b> — the historical behaviour, and the default. A value of, say,
    /// 200 corresponds to a contact-spot fraction of 5·10⁻³, which is inside the range reported for packed
    /// beds (10⁻³…10⁻²). It is a fixed calibration constant, NOT a fitted parameter: it adds no degree of
    /// freedom to the search, which is the only reason it can coexist with the δ ≤ d_AP constraint without
    /// making that constraint vacuous. A <i>fitted</i> multiplier here would be algebraically identical to
    /// removing the constraint, because δ enters the model nowhere else.</para>
    /// </summary>
    public required double SkeletonContactFactor { get; init; }
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

    /// <summary>
    /// Dimensionless contact-area correction on the skeleton conduction flux. See the ByDoubles twin,
    /// <see cref="MetalCombustionParamsByDoubles.SkeletonContactFactor"/>, for the physical reading and for
    /// why it must stay a fixed calibration constant rather than a fitted parameter. 1.0 = no correction.
    /// </summary>
    public required double SkeletonContactFactor { get; init; }
}

#endregion
