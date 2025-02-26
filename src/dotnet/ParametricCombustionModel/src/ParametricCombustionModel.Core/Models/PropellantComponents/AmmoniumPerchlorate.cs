using System.Text.Json.Serialization;

namespace ParametricCombustionModel.Core.Models.PropellantComponents;

public record AmmoniumPerchlorate(
    double MassFraction,
    double Density,
    [property: JsonPropertyName("large_particles_fraction")]
    [property: JsonRequired]
    double LargeParticlesFraction,
    [property: JsonPropertyName("average_particles_diameter")]
    [property: JsonRequired]
    double AverageParticlesDiameter
) : BaseComponent(MassFraction, Density)
{
    public double SmallParticlesFraction => 1.0 - LargeParticlesFraction;
}
