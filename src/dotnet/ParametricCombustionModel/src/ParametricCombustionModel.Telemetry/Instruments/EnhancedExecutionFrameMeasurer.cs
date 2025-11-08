using System.Diagnostics;

namespace ParametricCombustionModel.Telemetry.Instruments;

public class EnhancedExecutionFrameMeasurer : ExecutionFrameMeasurer
{
    private readonly Stopwatch _stopwatch = new();
    private double _dispersionExecutionTime;

    public double MinExecutionTime { get; private set; } = double.MaxValue;

    public double MaxExecutionTime { get; private set; } = double.MinValue;

    public uint CallsCount { get; private set; }

    public double MeanExecutionTime { get; private set; }

    public double StdDevExecutionTime => Math.Sqrt(_dispersionExecutionTime);
    
    public EnhancedExecutionFrameMeasurer(string name, string description, string unit, string path)
        : base(name, description, unit, path)
    {
    }

    public override ExecutionFrameMeasurer StartFrame()
    {
        _stopwatch.Restart();
        return this;
    }

    public override void EndFrame()
    {
        var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

        CallsCount++;

        if (elapsedMs < MinExecutionTime)
            MinExecutionTime = elapsedMs;

        if (elapsedMs > MaxExecutionTime)
            MaxExecutionTime = elapsedMs;

        var prevMeanExecutionTime = MeanExecutionTime;
        MeanExecutionTime += (elapsedMs - prevMeanExecutionTime) / CallsCount;
        _dispersionExecutionTime += ((elapsedMs - prevMeanExecutionTime) * (elapsedMs - MeanExecutionTime) -
                                     _dispersionExecutionTime) / CallsCount;
    }

    public override void Dispose() => EndFrame();
}
