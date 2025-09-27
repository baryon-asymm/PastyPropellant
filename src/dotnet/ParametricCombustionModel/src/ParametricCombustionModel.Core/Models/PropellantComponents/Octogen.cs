using System.Text.Json.Serialization;

namespace ParametricCombustionModel.Core.Models.PropellantComponents;

public record Octogen(
    double MassFraction,
    double Density
) : BaseComponent(MassFraction, Density);
