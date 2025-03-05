using System.Text.Json.Serialization;
using ParametricCombustionModel.Core.Models.GasPhases;

namespace ParametricCombustionModel.Core.Models;

public record PressureFrame(
    [property: JsonRequired]
    [property: JsonPropertyName("pressure")]
    double Pressure,
    [property: JsonRequired]
    [property: JsonPropertyName("porosity_within_skeleton")]
    double Porosity,
    [property: JsonPropertyName("inter_pocket_gas_phase")]
    [property: JsonRequired]
    HomogeneousGasPhase InterPocketGasPhase,
    [property: JsonPropertyName("pocket_gas_phase")]
    [property: JsonRequired]
    HeterogeneousGasPhase PocketGasPhase
);
