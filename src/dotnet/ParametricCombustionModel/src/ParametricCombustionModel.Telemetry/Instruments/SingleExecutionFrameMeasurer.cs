using System.Diagnostics;

namespace ParametricCombustionModel.Telemetry.Instruments;

public class SingleExecutionFrameMeasurer : ExecutionFrameMeasurer
{
    private readonly Stopwatch _stopwatch = new();

    public DateTime StartTime { get; } = DateTime.Now;

    public DateTime EndTime { get; private set; }

    public double ExecutionTime { get; private set; }

    public SingleExecutionFrameMeasurer(string name, string description, string unit, string path)
        : base(name, description, unit, path)
    {
        _stopwatch.Restart();
    }

    public override ExecutionFrameMeasurer StartFrame()
    {
        _stopwatch.Restart();
        return this;
    }

    public override void EndFrame()
    {
        _stopwatch.Stop();
        ExecutionTime = _stopwatch.Elapsed.TotalMilliseconds;
        EndTime = DateTime.Now;
    }

    public override void Dispose() => EndFrame();
}
