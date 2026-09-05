# API.md — Extensions

Пространство имён `ParametricCombustionModel.Optimization.Extensions`.

## Experimental burn rate ✅

```csharp
public static class PropellantExtensions
{
    // r = A * p^Nu; давление в паскалях.
    public static double GetExperimentalBurnRate(this Propellant propellant, double pressure);
    public static Speed GetExperimentalBurnRate(this Propellant propellant, Pressure pressure);
}
```
