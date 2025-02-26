namespace ParametricCombustionModel.Core.Models.PropellantComponents;

public record Aluminum(
    double MassFraction,
    double Density
) : BaseComponent(MassFraction, Density);
