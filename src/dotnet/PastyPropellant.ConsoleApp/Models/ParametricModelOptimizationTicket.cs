using System.Text.Json.Serialization;

namespace PastyPropellant.ConsoleApp.Models;

public enum StopCondition : byte
{
    IterationCountReached = 0,
    TargetFunctionValueCrossedBoundary
}

public enum InitialPointIterTransAction : byte
{
    TakeFirst = 0,
    TakeBest,
    GenerateRandom
}

public record ParametricModelOptimizationTicket(
    [property: JsonPropertyName("name")]
    [property: JsonRequired]
    string Name,
    [property: JsonPropertyName("propellants_file")]
    [property: JsonRequired]
    string PropellantsFilePath,
    [property: JsonPropertyName("worker_path")]
    [property: JsonRequired]
    string WorkerPath,
    [property: JsonPropertyName("num_trial_points")]
    int? NumTrialPoints,
    [property: JsonPropertyName("num_stage_one_points")]
    int? NumStageOnePoints,
    [property: JsonPropertyName("max_iter_time")]
    TimeSpan? MaxTime,
    [property: JsonPropertyName("pressures")]
    [property: JsonRequired]
    double[] Pressures,
    [property: JsonPropertyName("initial_point")]
    double[]? InitialPoint,
    [property: JsonPropertyName("lower_bound")]
    [property: JsonRequired]
    double[] LowerBound,
    [property: JsonPropertyName("upper_bound")]
    [property: JsonRequired]
    double[] UpperBound,
    [property: JsonPropertyName("min_surface_temp")]
    double? MinSurfaceTemperature,
    [property: JsonPropertyName("max_surface_temp")]
    double? MaxSurfaceTemperature,
    [property: JsonPropertyName("stop_condition")]
    StopCondition? StopCondition,
    [property: JsonPropertyName("iteration_number")]
    int? IterationNumber,
    [property: JsonPropertyName("max_target_function_value")]
    double? MaxTargetFunctionValue,
    [property: JsonPropertyName("initial_point_iter_trans_action")]
    InitialPointIterTransAction? InitialPointAction,
    [property: JsonPropertyName("offset_over_middle_heat_flow")]
    double? OffsetOverMiddleHeatFlow
);
