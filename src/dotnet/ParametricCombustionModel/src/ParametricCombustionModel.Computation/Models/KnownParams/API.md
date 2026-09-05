# API.md — KnownParams

Пространство имён `ParametricCombustionModel.Computation.Models.KnownParams`.
Всё объявлено парами `…ByDoubles` / `…ByUnits`; ниже приводится один ярус, если
второй отличается только типами величин.

## Solver parameter vector, one composition ✅

```csharp
public readonly ref struct CombustionSolverParamsByDoubles
{
    public required double ADecompose { get; init; }
    public required double EDecompose { get; init; }
    public required double AKineticFlameInterPocket { get; init; }
    public required double EKineticFlameInterPocket { get; init; }
    public required double AKineticFlamePocketOutSkeleton { get; init; }
    public required double EKineticFlamePocketOutSkeleton { get; init; }
    public required double AKineticFlamePocketSkeleton { get; init; }
    public required double EKineticFlamePocketSkeleton { get; init; }
    public required double NuInterPocket { get; init; }
    public required double NuPocketOutSkeleton { get; init; }
    public required double NuPocketSkeleton { get; init; }
    public required double AMetalBurningConstant { get; init; }
    public required double BMetalBurningConstant { get; init; }
    public required double DeltaH { get; init; }
    public required double KDiffusionHeight { get; init; }
    public required double APowOrder { get; init; }
    public required double BPowOrder { get; init; }
    public required double KCoefficientRadiationTemperature { get; init; }

    public static CombustionSolverParamsByDoubles FromVector(ReadOnlySpan<double> vector);
}
```

Размерный двойник `CombustionSolverParamsByUnits` несёт тот же набор с
величинами UnitsNet (`MassFlux`, `MolarEnergy`, `Frequency`, `SpecificEnergy`)
и с `AMetalBurningConstant` / `BMetalBurningConstant` из
[Units](../../Units/API.md), плюс собственный `FromVector`.

## Solver parameter vector, three compositions ✅

```csharp
public readonly ref struct GroupCombustionSolverParamsByDoubles
{
    // [0..10] общие для всех композиций
    public required double AKineticFlameInterPocket { get; init; }
    public required double EKineticFlameInterPocket { get; init; }
    public required double AKineticFlamePocketOutSkeleton { get; init; }
    public required double EKineticFlamePocketOutSkeleton { get; init; }
    public required double NuInterPocket { get; init; }
    public required double NuPocketOutSkeleton { get; init; }
    public required double DeltaH { get; init; }
    public required double KDiffusionHeight { get; init; }
    public required double APowOrder { get; init; }
    public required double BPowOrder { get; init; }
    public required double KCoefficientRadiationTemperature { get; init; }

    // [11..17] Bas_2 + Bas_3 + Bas_4, [18..24] Bas_1, [25..31] Bas_0
    public required double ADecomposeBas2 { get; init; }
    public required double EDecomposeBas2 { get; init; }
    public required double AKineticFlamePocketSkeletonBas2 { get; init; }
    public required double EKineticFlamePocketSkeletonBas2 { get; init; }
    public required double NuPocketSkeletonBas2 { get; init; }
    public required double AMetalBurningConstantBas2 { get; init; }
    public required double BMetalBurningConstantBas2 { get; init; }
    public required double ADecomposeBas1 { get; init; }
    public required double EDecomposeBas1 { get; init; }
    public required double AKineticFlamePocketSkeletonBas1 { get; init; }
    public required double EKineticFlamePocketSkeletonBas1 { get; init; }
    public required double NuPocketSkeletonBas1 { get; init; }
    public required double AMetalBurningConstantBas1 { get; init; }
    public required double BMetalBurningConstantBas1 { get; init; }
    public required double ADecomposeBas0 { get; init; }
    public required double EDecomposeBas0 { get; init; }
    public required double AKineticFlamePocketSkeletonBas0 { get; init; }
    public required double EKineticFlamePocketSkeletonBas0 { get; init; }
    public required double NuPocketSkeletonBas0 { get; init; }
    public required double AMetalBurningConstantBas0 { get; init; }
    public required double BMetalBurningConstantBas0 { get; init; }

    public static GroupCombustionSolverParamsByDoubles FromVector(ReadOnlySpan<double> vector);
    public double[] ToCompositionVector(int compositionIndex);   // 0 -> Bas_2, 1 -> Bas_1, 2 -> Bas_0
}
```

`GroupCombustionSolverParamsByUnits` — размерный двойник с той же раскладкой.

## Propellant and flame inputs ✅

```csharp
public readonly struct PropellantParamsByDoubles
{
    public required double SpecificHeatCapacity { get; init; }
    public required double Density { get; init; }
    public required double InitialTemperature { get; init; }
    public required double AverageOxidizerDiameter { get; init; }
    public required SkeletonCoverageCurve SkeletonCoverage { get; init; }
}

public readonly struct KineticFlameParamsByDoubles
{
    public required double FinalTemperature { get; init; }
    public required double AverageMolarMass { get; init; }
    public required double ThermalConductivity { get; init; }
    public required double VolumetricSpecificHeatCapacity { get; init; }
}

public readonly struct DiffusionFlameParamsByDoubles { /* тот же набор полей */ }

public readonly struct MetalCombustionParamsByDoubles
{
    public required double MetalMeltingTemperature { get; init; }
    public required double MetalBoilingTemperature { get; init; }
    public required double SkeletonContactFactor { get; init; }
}

public readonly struct SkeletonLayerParamsByDoubles
{
    public required double Porosity { get; init; }
    public required double CondensedThermalConductivity { get; init; }
}
```

Размерные двойники — `PropellantParamsByUnits`, `KineticFlameParamsByUnits`,
`DiffusionFlameParamsByUnits`, `MetalCombustionParamsByUnits`,
`SkeletonLayerParamsByUnits`. У двух из них (`DiffusionFlameParamsByUnits`,
`MetalCombustionParamsByUnits`) температура объявлена с `set`.

## Run constants ✅

```csharp
public sealed record ModelConstants
{
    public double MetalMeltingTemperatureKelvins { get; init; }   // 1300
    public double SkeletonContactFactor { get; init; }            // 1.0 — без поправки
    public SkeletonSurfaceFractionSettings SkeletonSurfaceFraction { get; init; }
    public double MinSurfaceTemperatureKelvins { get; init; }     // 600
    public double MaxSurfaceTemperatureKelvins { get; init; }     // 900

    public static ModelConstants Default { get; }
    public void Validate();
}
```

## Skeleton coverage closure ✅

```csharp
public enum SkeletonSurfaceFractionMode { Polynomial, EquilibriumCarbon, KineticCoverage }

public sealed record SkeletonSurfaceFractionSettings
{
    public SkeletonSurfaceFractionMode Mode { get; init; }          // Polynomial
    public SkeletonCarbonEquilibriumTable? EquilibriumTable { get; init; }
    public KineticCoverageSettings Kinetic { get; init; }
    public static SkeletonSurfaceFractionSettings Default { get; }
    public void Validate();
}

public sealed record KineticCoverageSettings
{
    public double BinderChannel { get; init; }            // a0 = 0.9966
    public double FineOxidiserChannel { get; init; }      // a1 = 3.3653
    public double PressureOrder { get; init; }            // m  = 0.71
    public double ReferencePressurePascals { get; init; } // 1e6
    public static KineticCoverageSettings Default { get; }
    public double CoverageAt(double fineOxidiserMassFraction, double pressurePascals);
    public void Validate();
}

public sealed class SkeletonCoverageCurve
{
    public static SkeletonCoverageCurve Constant(double coverage);
    public double At(double surfaceTemperatureKelvins);   // линейно внутри, зажим по краям
}

public sealed class SkeletonCarbonEquilibriumTable
{
    public string Source { get; }
    public IEnumerable<string> PropellantNames { get; }
    public SkeletonCoverageCurve CurveFor(string propellantName, double pressurePascals);  // бросает, если нет
    public static SkeletonCarbonEquilibriumTable Load(string path);
}
```
