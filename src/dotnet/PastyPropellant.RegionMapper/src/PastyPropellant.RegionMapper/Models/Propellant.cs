using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PastyPropellant.RegionMapper.Models;

public record Propellant(
    [property: Required]
    [property: JsonPropertyName("name")]
    string Name
);
