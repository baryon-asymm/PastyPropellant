using System.Diagnostics;
using ParametricCombustionModel.Telemetry.Interfaces;

namespace ParametricCombustionModel.Telemetry.MetricsRecorders;

public class FitnessFunctionMetricsRecorder : IExecutionFrameMeasurer
{
    private readonly Stopwatch _stopwatch = new();
    private double _dispersionExecutionTime;

    public uint CallsCount { get; private set; }

    public double MeanExecutionTime { get; private set; }

    public double StdDevExecutionTime => Math.Sqrt(_dispersionExecutionTime);

    public void StartFrame()
    {
        _stopwatch.Restart();
    }

    public void EndFrame()
    {
        var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

        var prevMeanExecutionTime = MeanExecutionTime;
        MeanExecutionTime += (elapsedMs - prevMeanExecutionTime) / CallsCount;
        _dispersionExecutionTime += ((elapsedMs - prevMeanExecutionTime) * (elapsedMs - MeanExecutionTime) -
                                     _dispersionExecutionTime) / CallsCount;
    }
}
