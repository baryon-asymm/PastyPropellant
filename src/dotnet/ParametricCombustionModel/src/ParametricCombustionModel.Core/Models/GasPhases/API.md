# API.md — GasPhases

Пространство имён `ParametricCombustionModel.Core.Models.GasPhases`.
Обе записи — позиционные, разбираются `System.Text.Json` по именам из
атрибутов `[JsonPropertyName]`, указанным в комментариях.

## Homogeneous gas phase ✅

```csharp
public record HomogeneousGasPhase(
    double Lambda_Gas,                    // "lambda_gas"
    double AverageMolarMass,              // "average_molar_mass"
    double SpecificHeatCapacity_Volume,   // "c_volume"
    double KineticFlameTemperature);      // "T_kinetic_flame"
```

## Heterogeneous gas phase ✅

```csharp
public record HeterogeneousGasPhase(
    double Lambda_Gas,                            // "lambda_gas"
    double AverageMolarMass,                      // "average_molar_mass"
    double SpecificHeatCapacity_Volume,           // "c_volume"
    double DiffusionFlameTemperature,             // "T_diffusion_flame"
    HomogeneousGasPhase SkeletonGasPhase,         // "skeleton_gas_phase"
    HomogeneousGasPhase OutSkeletonGasPhase);     // "out_skeleton_gas_phase"
```
