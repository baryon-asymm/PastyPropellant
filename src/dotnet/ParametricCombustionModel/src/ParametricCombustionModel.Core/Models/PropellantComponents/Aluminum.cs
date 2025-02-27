using System.Text.Json.Serialization;

namespace ParametricCombustionModel.Core.Models.PropellantComponents;

public record Aluminum(
    double MassFraction,
    double Density,
    [property: JsonRequired]
    [property: JsonPropertyName("agglomeration_coefficients")]
    IEnumerable<double> AgglomerationCoefficients
) : BaseComponent(MassFraction, Density);
