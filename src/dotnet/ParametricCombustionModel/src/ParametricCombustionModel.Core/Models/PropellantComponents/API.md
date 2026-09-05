# API.md — PropellantComponents

Пространство имён `ParametricCombustionModel.Core.Models.PropellantComponents`.
Имена свойств JSON — в комментариях; позиционные параметры `MassFraction` и
`Density` наследуются от базовой записи.

## Component root ✅

```csharp
public abstract record BaseComponent(
    double MassFraction,   // "mass_fraction"
    double Density);       // "density"
```

## Components ✅

```csharp
public record CombustibleBinder(double MassFraction, double Density)
    : BaseComponent(MassFraction, Density);

public record Octogen(double MassFraction, double Density)
    : BaseComponent(MassFraction, Density);

public record AmmoniumPerchlorate(
    double MassFraction,
    double Density,
    double LargeParticlesFraction,       // "large_particles_fraction"
    double AverageParticlesDiameter)     // "average_particles_diameter"
    : BaseComponent(MassFraction, Density)
{
    public double SmallParticlesFraction { get; }   // 1 - LargeParticlesFraction
}

public record Aluminum(
    double MassFraction,
    double Density,
    IEnumerable<double> AgglomerationCoefficients,                            // "agglomeration_coefficients"
    IEnumerable<ConfidenceInterval>? AgglomerationConfidenceIntervals = null)  // "agglomeration_confidence_intervals"
    : BaseComponent(MassFraction, Density);
```
