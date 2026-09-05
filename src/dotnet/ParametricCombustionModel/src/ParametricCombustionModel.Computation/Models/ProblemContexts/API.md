# API.md — ProblemContexts

Пространство имён
`ParametricCombustionModel.Computation.Models.ProblemContexts`.

## Search bracket ✅

```csharp
public static class SurfaceTemperatureSearchBounds
{
    public const double MinKelvins = 600;
    public const double MaxKelvins = 900;
}
```

## Problem context, dimensional tier ✅

```csharp
public record ProblemContextByUnits : IComputationVisitable
{
    public required Propellant Propellant;
    public required Pressure Pressure;
    public required PropellantParamsByUnits PropellantParamsByUnits;
    public required KineticFlameParamsByUnits InterPocketKineticFlameParamsByUnits;
    public required KineticFlameParamsByUnits PocketSkeletonKineticFlameParamsByUnits;
    public required KineticFlameParamsByUnits PocketOutSkeletonKineticFlameParamsByUnits;
    public required DiffusionFlameParamsByUnits PocketDiffusionFlameParamsByUnits;
    public required MetalCombustionParamsByUnits PocketMetalCombustionParamsByUnits;
    public required SkeletonLayerParamsByUnits SkeletonLayerParamsByUnits;
    public required Ratio InterPocketVolumeFraction;
    public required Ratio PocketVolumeFraction;
    public required MixedCombustionParams MixedCombustionParams;
    public required InterPocketCombustionParams InterPocketCombustionParams;
    public required PocketCombustionParams PocketCombustionParams;
    public Temperature MinSurfaceTemperature;
    public Temperature MaxSurfaceTemperature;

    public void Accept(in CombustionSolverParamsByUnits solverParamsByUnits, ISolverVisitor solver);
    public void Accept(in CombustionSolverParamsByDoubles solverParams, ISolverVisitor solver);  // бросает
}
```

## Problem context, double tier ✅

```csharp
public class ProblemContextByDoubles : IComputationVisitable
{
    public required Propellant Propellant;
    public required double Pressure;
    public required PropellantParamsByDoubles PropellantParams;
    public required KineticFlameParamsByDoubles InterPocketKineticFlameParams;
    public required KineticFlameParamsByDoubles PocketSkeletonKineticFlameParams;
    public required KineticFlameParamsByDoubles PocketOutSkeletonKineticFlameParams;
    public required DiffusionFlameParamsByDoubles PocketDiffusionFlameParams;
    public required MetalCombustionParamsByDoubles PocketMetalCombustionParams;
    public required SkeletonLayerParamsByDoubles SkeletonLayerParams;
    public required double InterPocketVolumeFraction;
    public required double PocketVolumeFraction;
    public required MixedCombustionParamsByDoubles MixedCombustionParams;
    public required InterPocketCombustionParamsByDoubles InterPocketCombustionParams;
    public required PocketCombustionParamsByDoubles PocketCombustionParams;
    public double MinSurfaceTemperature;
    public double MaxSurfaceTemperature;

    public void Accept(in CombustionSolverParamsByUnits solverParamsByUnits, ISolverVisitor solver);  // бросает
    public void Accept(in CombustionSolverParamsByDoubles solverParams, ISolverVisitor solver);
}
```
