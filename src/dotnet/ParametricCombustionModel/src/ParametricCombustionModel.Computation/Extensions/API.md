# API.md — Extensions

Пространство имён `ParametricCombustionModel.Computation.Extensions`.
Все методы — расширения `Propellant`; давление везде в паскалях.

## Recipe-derived quantities ✅

```csharp
public static class PropellantExtensions
{
    public static double GetInterPocketAreaVolumeFraction(this Propellant propellant);
    public static double GetPocketAreaVolumeFraction(this Propellant propellant);
    public static double GetAverageParticlesDiameter(this Propellant propellant);
    public static double GetFineOxidiserMassFraction(this Propellant propellant);
    public static double GetPocketSurfaceFraction(this Propellant propellant, double pressure);
    public static double GetMetalBoilingTemperature(this Propellant propellant, double pressure);

    // Единственная точка входа к поверхностной доле каркаса; settings == null -> полином.
    public static SkeletonCoverageCurve GetSkeletonCoverage(
        this Propellant propellant, double pressure, SkeletonSurfaceFractionSettings? settings);

    // Значение ПО УМОЛЧАНИЮ для ModelConstants; из расчётного кода не читать.
    public const double MetalMeltingTemperatureKelvins = 1300;
}
```
