using System.Text.Json.Serialization;

namespace ParametricCombustionModel.Core.Models.GasPhases;

public record HeterogeneousGasPhase(
    [property: JsonPropertyName("lambda_gas")]
    [property: JsonRequired]
    double Lambda_Gas,
    [property: JsonPropertyName("average_molar_mass")]
    [property: JsonRequired]
    double AverageMolarMass,
    [property: JsonPropertyName("c_volume")]
    [property: JsonRequired]
    double SpecificHeatCapacity_Volume,
    [property: JsonPropertyName("T_diffusion_flame")]
    [property: JsonRequired]
    double DiffusionFlameTemperature,
    [property: JsonPropertyName("skeleton_gas_phase")]
    [property: JsonRequired]
    HomogeneousGasPhase SkeletonGasPhase,
    [property: JsonPropertyName("out_skeleton_gas_phase")]
    [property: JsonRequired]
    HomogeneousGasPhase OutSkeletonGasPhase
);
