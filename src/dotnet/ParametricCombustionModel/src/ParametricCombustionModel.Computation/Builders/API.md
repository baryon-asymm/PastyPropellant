# API.md — Builders

Пространство имён `ParametricCombustionModel.Computation.Builders`.
Два зеркальных сборщика, по одному на ярус.

## Matrix builders ✅

```csharp
public class ProblemContextByUnitsMatrixBuilder
{
    public IEnumerable<Propellant> Propellants { get; init; }
    public IEnumerable<Pressure> Pressures { get; private set; }   // из PressureFrames первого топлива
    public ModelConstants ModelConstants { get; }

    public ProblemContextByUnits[,] BuildMatrix();                 // [топливо, давление]

    public static ProblemContextByUnitsMatrixBuilder FromPropellants(
        IEnumerable<Propellant> propellants,
        ModelConstants? modelConstants = null);                    // null -> ModelConstants.Default
}

public class ProblemContextByDoublesMatrixBuilder
{
    public IEnumerable<Propellant> Propellants { get; init; }
    public IEnumerable<Pressure> Pressures { get; private set; }
    public ModelConstants ModelConstants { get; }

    public ProblemContextByDoubles[,] BuildMatrix();

    public static ProblemContextByDoublesMatrixBuilder FromPropellants(
        IEnumerable<Propellant> propellants,
        ModelConstants? modelConstants = null);
}
```
