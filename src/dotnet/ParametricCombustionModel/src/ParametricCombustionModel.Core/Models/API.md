# API.md — Models

Пространство имён `ParametricCombustionModel.Core.Models`. Имена свойств JSON —
в комментариях.

## Propellant ✅

```csharp
public record Propellant(
    string Name,                                                // "name"
    double A,                                                   // "a"
    double Nu,                                                  // "nu"
    IEnumerable<BaseComponent> Components,                      // "components"
    double Density,                                             // "density"
    double SpecificHeatCapacity,                                // "specific_heat_capacity"
    double InitialTemperature,                                  // "initial_temperature"
    IEnumerable<double> PocketSurfaceFractionCoefficients,      // "pocket_surface_fraction_coefficients"
    IEnumerable<ConfidenceInterval>? ConfidenceIntervals,       // "confidence_intervals"
    double PocketMassFraction,                                  // "pocket_mass_fraction"
    IEnumerable<PressureFrame>? PressureFrames);                // "pressure_frames"
```

## Measurement with error bars ✅

```csharp
public record ConfidenceInterval(
    double XValue,                    // "x_value"
    double YValue,                    // "y_value"
    double SizeOfConfidenceInterval); // "size_of_confidence_interval" — ПОЛНАЯ высота уса
```

## Pressure frame ✅

```csharp
public record PressureFrame(
    double Pressure,                              // "pressure"
    double Porosity,                              // "porosity_within_skeleton"
    HomogeneousGasPhase InterPocketGasPhase,      // "inter_pocket_gas_phase"
    HeterogeneousGasPhase PocketGasPhase);        // "pocket_gas_phase"
```

## Component reader ✅

```csharp
public class PropellantComponentJsonConverter : JsonConverter<IEnumerable<BaseComponent>>
{
    public override IEnumerable<BaseComponent>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
    public override void Write(
        Utf8JsonWriter writer, IEnumerable<BaseComponent> value, JsonSerializerOptions options);
}
```

## Children

- [GasPhases/API.md](./GasPhases/API.md) — состояние газовой фазы;
- [PropellantComponents/API.md](./PropellantComponents/API.md) — компоненты рецептуры.
