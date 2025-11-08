using ParametricCombustionModel.Telemetry.Instruments;

namespace ParametricCombustionModel.Telemetry;

public class PerformanceMeter
{
    public SingleExecutionFrameMeasurer? TotalExecutionTimeMeasurer { get; private set; }

    private readonly List<EnhancedExecutionFrameMeasurer> _workloadExecutionFrames = new();

    public IReadOnlyList<EnhancedExecutionFrameMeasurer> ExecutionFrames => _workloadExecutionFrames.AsReadOnly();

    public SingleExecutionFrameMeasurer GetTotalExecutionTimeMeasurer()
    {
        if (TotalExecutionTimeMeasurer is null)
        {
            TotalExecutionTimeMeasurer = new SingleExecutionFrameMeasurer(
                "TotalExecutionTime",
                "Total execution time of the measured workload",
                "ms",
                "/Performance/TotalExecutionTime");
        }

        return TotalExecutionTimeMeasurer;
    }

    public EnhancedExecutionFrameMeasurer CreateExecutionFrameMeasurer(string name, string description, string unit, string path)
    {
        var measurer = new EnhancedExecutionFrameMeasurer(name, description, unit, path);
        _workloadExecutionFrames.Add(measurer);
        return measurer;
    }
}
