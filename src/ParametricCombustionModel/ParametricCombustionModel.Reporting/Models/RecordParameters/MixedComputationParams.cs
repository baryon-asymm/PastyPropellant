namespace ParametricCombustionModel.Reporting.Models.RecordParameters;

public readonly struct MixedComputationParams
{
    public double BurningRate { get; init; }
    public double InterPocketVolumeFraction { get; init; }
    public double PocketVolumeFraction { get; init; }
    public double InterPocketBurningRateFraction { get; init; }
    public double PocketBurningRateFraction { get; init; }
    public InterPocketComputationParams InterPocketComputationParams { get; init; }
    public PocketComputationParams PocketComputationParams { get; init; }
}
