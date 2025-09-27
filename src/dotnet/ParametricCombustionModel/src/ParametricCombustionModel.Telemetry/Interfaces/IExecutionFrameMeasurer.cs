namespace ParametricCombustionModel.Telemetry.Interfaces;

public interface IExecutionFrameMeasurer
{
    public uint CallsCount { get; }

    public double MeanExecutionTime { get; }

    public double StdDevExecutionTime { get; }

    public void StartFrame();

    public void EndFrame();
}
