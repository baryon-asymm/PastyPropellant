using UnitsNet;

namespace ParametricCombustionModel.Computation.Models.KnownParams;

/// <summary>
/// Represents a set of kinetic flame parameters for modeling combustion processes in a kinetic flame model.
/// This struct includes properties such as the final temperature of the flame, average molar mass of the gas mixture,
/// thermal conductivity, and volumetric specific heat capacity. These parameters are essential for accurately characterizing
/// the behavior and properties of combustion gases in kinetic flame models.
/// </summary>
public readonly struct KineticFlameParams
{
#region Properties

    /// <summary>
    /// Gets the final temperature of the kinetic flame, which is crucial for characterizing the combustion process.
    /// The final temperature determines the thermal state of the combustion products and affects reaction rates and energy release.
    /// </summary>
    public required Temperature FinalTemperature { get; init; }

    /// <summary>
    /// Gets the average molar mass of the gas mixture involved in the kinetic flame.
    /// This property is important for understanding the mass-based properties of the combustion gases, including diffusion rates
    /// and molecular interactions within the flame.
    /// </summary>
    public required MolarMass AverageMolarMass { get; init; }

    /// <summary>
    /// Gets the thermal conductivity of the gas mixture in the kinetic flame.
    /// Thermal conductivity measures the ability of the gas mixture to conduct heat, which is critical for modeling heat transfer
    /// processes and temperature distributions within the flame.
    /// </summary>
    public required ThermalConductivity ThermalConductivity { get; init; }

    /// <summary>
    /// Gets the volumetric specific heat capacity of the gas mixture in the kinetic flame.
    /// The volumetric specific heat capacity (often referred to as specific entropy in volumetric terms) measures how much heat
    /// energy the gas mixture can store per unit volume, which is important for energy balance calculations and understanding
    /// the thermal dynamics of the flame.
    /// </summary>
    public required SpecificEntropy VolumetricSpecificHeatCapacity { get; init; }

#endregion
}
