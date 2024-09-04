using System.Text.Json.Serialization;

namespace PastyPropellant.Core.Models.Thermodynamic;

public enum SubstancePhase : byte
{
    Liquid,
    Gas
}

public record TemperatureRange(
    [property: JsonPropertyName("min")]
    [property: JsonRequired]
    double Min,
    [property: JsonPropertyName("max")]
    [property: JsonRequired]
    double Max
);

public record ThermodynamicSubstance(
    [property: JsonPropertyName("formula")]
    [property: JsonRequired]
    string Formula,
    [property: JsonPropertyName("coefficients")]
    [property: JsonRequired]
    List<double> Coefficients,
    [property: JsonPropertyName("phase")]
    [property: JsonRequired]
    SubstancePhase Phase,
    [property: JsonPropertyName("temperature_range")]
    [property: JsonRequired]
    TemperatureRange TemperatureRange
);
