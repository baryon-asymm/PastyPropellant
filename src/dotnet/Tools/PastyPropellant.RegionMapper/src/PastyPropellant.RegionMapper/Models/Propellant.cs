using System.Text.Json.Serialization;

namespace PastyPropellant.RegionMapper.Models;

public record Propellant(
    [property: JsonRequired]
    [property: JsonPropertyName("name")]
    string Name
);
