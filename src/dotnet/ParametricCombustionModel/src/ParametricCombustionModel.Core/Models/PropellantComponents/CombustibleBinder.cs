namespace ParametricCombustionModel.Core.Models.PropellantComponents;

public record CombustibleBinder(
    double MassFraction,
    double Density
) : BaseComponent(MassFraction, Density);
