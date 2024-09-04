namespace ParametricCombustionModel.ParamsRecording.Models;

public readonly struct InterPocketComputationParams
{
    public double SurfaceTemperature { get; init; }
    public double BurningRate { get; init; }
    public double DecomposingRate { get; init; }
    public double KineticFlameHeatFlow { get; init; }
    public double KineticFlameHeight { get; init; }
}
