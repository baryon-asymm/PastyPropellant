namespace ParametricCombustionModel.Telemetry.Instruments;

public abstract class ExecutionFrameMeasurer : Instrument, IDisposable
{
    public ExecutionFrameMeasurer(string name, string description, string unit, string path)
        : base(name, description, unit, path)
    {
    }

    public abstract ExecutionFrameMeasurer StartFrame();

    public abstract void EndFrame();

    public abstract void Dispose();
}
