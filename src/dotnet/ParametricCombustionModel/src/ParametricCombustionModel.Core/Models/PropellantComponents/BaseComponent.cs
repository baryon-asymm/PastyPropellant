using System.Text.Json.Serialization;

namespace ParametricCombustionModel.Core.Models.PropellantComponents;

public abstract record BaseComponent(
    [property: JsonPropertyName("mass_fraction")]
    [property: JsonRequired]
    double MassFraction,
    [property: JsonPropertyName("density")]
    [property: JsonRequired]
    double Density
);
