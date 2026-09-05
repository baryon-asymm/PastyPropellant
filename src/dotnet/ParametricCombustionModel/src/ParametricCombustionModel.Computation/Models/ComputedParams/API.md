# API.md — ComputedParams

Пространство имён `ParametricCombustionModel.Computation.Models.ComputedParams`.
Каждый результат объявлен парой: `…ByDoubles` для горячего пути и размерный
двойник для отчётов. Ниже показан размерный ярус; ярус `double` имеет тот же
набор полей с типом `double`.

## Kinetic flame ✅

```csharp
public struct KineticFlameCombustionParamsByDoubles
{
    public double KineticFlameHeight;
    public double AverageKineticFlameDensity;
    public double AverageKineticFlameTemperature;
    public double KineticFlameHeatFlux;
}

public struct KineticFlameCombustionParams
{
    public Length KineticFlameHeight;
    public Density AverageKineticFlameDensity;
    public Temperature AverageKineticFlameTemperature;
    public HeatFlux KineticFlameHeatFlux;
}
```

## Inter-pocket region ✅

```csharp
public struct InterPocketCombustionParams
{
    public bool BurnRateIsFound;
    public Temperature SurfaceTemperature;
    public Speed BurnRate;
    public MassFlux DecomposeRate;
    public KineticFlameCombustionParams KineticFlameCombustionParams;
    public HeatFlux SublimationHeatFlux;
    public HeatFlux SurfaceHeatFluxesError;
}

public struct InterPocketCombustionParamsByDoubles { /* те же поля, double */ }
```

## Pocket ✅

```csharp
public struct PocketCombustionParams
{
    public bool BurnRateIsFound;
    public Temperature SurfaceTemperature;
    public Speed BurnRate;
    public MassFlux DecomposeRate;
    public KineticFlameCombustionParams OutSkeletonKineticFlameCombustionParams;
    public KineticFlameCombustionParams SkeletonKineticFlameCombustionParams;
    public ThermalConductivity EffectiveThermalConductivity;
    public ThermalConductivity ConductiveThermalConductivity;
    public double ConductiveThermalConductivityBalanceError;   // невязка, безразмерна в обоих ярусах
    public ThermalConductivity RadiativeThermalConductivity;
    public Length SkeletonLayerThickness;
    public Length PoreDiameter;
    public HeatFlux MetalBurningHeatFlux;
    public Length DiffusionFlameHeight;
    public HeatFlux DiffusionFlameHeatFlux;
    public HeatFlux OutSkeletonHeatFlux;
    public HeatFlux SkeletonHeatFlux;
    public HeatFlux ToSurfaceTotalHeatFlux;
    public HeatFlux SublimationHeatFlux;
    public HeatFlux SurfaceHeatFluxesError;
}

public struct PocketCombustionParamsByDoubles { /* те же поля, double */ }
```

## Mixed result ✅

```csharp
public struct MixedCombustionParams
{
    public bool BurnRateIsFound;
    public Speed BurnRate;
}

public struct MixedCombustionParamsByDoubles
{
    public bool BurnRateIsFound;
    public double BurnRate;
}
```
