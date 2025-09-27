using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

#region Utilization of Doubles

/// <summary>
/// Represents a set of thermodynamic parameters for modeling diffusion flame combustion processes using native double values.
/// This struct includes properties such as the final temperature of the diffusion flame, average molar mass,
/// thermal conductivity, and volumetric specific heat capacity. These parameters are essential for accurately
/// characterizing the behavior and properties of combustion gases in diffusion flames.
/// </summary>
public readonly struct DiffusionFlameParamsByDoubles
{
    /// <summary>
    /// Gets the final temperature of the diffusion flame, which is crucial for characterizing the combustion process.
    /// The final temperature is a key parameter in determining the thermal state of the combustion products.
    /// Measured in Kelvin (K).
    /// </summary>
    public required double FinalTemperature { get; init; }

    /// <summary>
    /// Gets the average molar mass of the gas mixture involved in the diffusion flame.
    /// This property is important for understanding the mass-based properties of the combustion gases, which
    /// influence the diffusion rates, heat capacity, and other thermodynamic characteristics.
    /// Measured in kg/mol.
    /// </summary>
    public required double AverageMolarMass { get; init; }

    /// <summary>
    /// Gets the thermal conductivity of the gas mixture in the diffusion flame.
    /// Thermal conductivity is a measure of the material's ability to conduct heat and is key to modeling heat transfer
    /// processes in combustion. A higher thermal conductivity indicates better heat distribution through the gas mixture.
    /// Measured in W/(m*K).
    /// </summary>
    public required double ThermalConductivity { get; init; }

    /// <summary>
    /// Gets the volumetric specific heat capacity of the gas mixture in the diffusion flame.
    /// The volumetric specific heat capacity is a measure of how much heat energy the gas mixture can store per unit volume
    /// and is vital for energy balance calculations in combustion modeling.
    /// Measured in J/(m^3*K).
    /// </summary>
    public required double VolumetricSpecificHeatCapacity { get; init; }
}

#endregion

#region Utilization of UnitsNet

/// <summary>
/// Represents a set of thermodynamic parameters for modeling diffusion flame combustion processes.
/// This struct includes properties such as the final temperature of the diffusion flame, average molar mass,
/// thermal conductivity, and volumetric specific heat capacity. These parameters are essential for accurately
/// characterizing the behavior and properties of combustion gases in diffusion flames.
/// </summary>
public struct DiffusionFlameParamsByUnits
{
    /// <summary>
    /// Gets the final temperature of the diffusion flame, which is crucial for characterizing the combustion process.
    /// The final temperature is a key parameter in determining the thermal state of the combustion products.
    /// </summary>
    public required Temperature FinalTemperature { get; set; }

    /// <summary>
    /// Gets the average molar mass of the gas mixture involved in the diffusion flame.
    /// This property is important for understanding the mass-based properties of the combustion gases, which
    /// influence the diffusion rates, heat capacity, and other thermodynamic characteristics.
    /// </summary>
    public required MolarMass AverageMolarMass { get; init; }

    /// <summary>
    /// Gets the thermal conductivity of the gas mixture in the diffusion flame.
    /// Thermal conductivity is a measure of the material's ability to conduct heat and is key to modeling heat transfer
    /// processes in combustion. A higher thermal conductivity indicates better heat distribution through the gas mixture.
    /// </summary>
    public required ThermalConductivity ThermalConductivity { get; init; }

    /// <summary>
    /// Gets the volumetric specific heat capacity of the gas mixture in the diffusion flame.
    /// The volumetric specific heat capacity (often referred to as specific entropy in volumetric terms) is a measure
    /// of how much heat energy the gas mixture can store per unit volume and is vital for energy balance calculations
    /// in combustion modeling.
    /// </summary>
    public required SpecificEntropy VolumetricSpecificHeatCapacity { get; init; }
}

#endregion
