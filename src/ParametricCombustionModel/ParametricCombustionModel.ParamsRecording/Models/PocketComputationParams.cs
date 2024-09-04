namespace ParametricCombustionModel.ParamsRecording.Models;

public readonly struct PocketComputationParams
{
    public double SurfaceTemperature { get; init; }
    public double BurningRate { get; init; }
    public double DecomposingRate { get; init; }
    public double SkeletonKineticFlameHeatFlow { get; init; }
    public double SkeletonKineticFlameHeight { get; init; }
    public double OutSkeletonKineticFlameHeatFlow { get; init; }
    public double OutSkeletonKineticFlameHeight { get; init; }
    public double DiffusionFlameHeatFlow { get; init; }
    public double MetalBurningHeatFlow { get; init; }
    public double PocketSurfaceFraction { get; init; }
}
