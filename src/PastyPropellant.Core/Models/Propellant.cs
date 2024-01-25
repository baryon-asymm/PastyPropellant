namespace PastyPropellant.Core.Models;

public record Propellant
{
    public string Name { get; init; }
    public double A { get; init; }
    public double Nu { get; init; }
}
