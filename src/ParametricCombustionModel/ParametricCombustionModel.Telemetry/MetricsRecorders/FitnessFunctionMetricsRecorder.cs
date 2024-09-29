using System.Diagnostics;
using System.Runtime.CompilerServices;
using ParametricCombustionModel.Computation.Solvers;

namespace ParametricCombustionModel.Telemetry.MetricsRecorders;

public class FitnessFunctionMetricsRecorder : FitnessFunctionNonlconEvaluator
{
    private readonly Stopwatch _stopwatch = new();
    private double _dispersionExecutionTime;

    public FitnessFunctionMetricsRecorder(IEnumerable<IEnumerable<double>> experimentalBurningRates,
                                         IEnumerable<IEnumerable<MixedPropellantSolver>> mixedPropellantSolvers,
                                         (double, double) surfaceTemperatureRange)
        : base(experimentalBurningRates,
               mixedPropellantSolvers,
               surfaceTemperatureRange)
    {
    }

    public uint TargetFunctionCallsCount { get; private set; }

    public double MeanExecutionTime { get; private set; }

    public double StdDevExecutionTime => Math.Sqrt(_dispersionExecutionTime);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Evaluate(Span<double> x, Span<double> values)
    {
        TargetFunctionCallsCount++;

        _stopwatch.Restart();
        base.Evaluate(x, values);
        var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

        var prevMeanExecutionTime = MeanExecutionTime;
        MeanExecutionTime += (elapsedMs - prevMeanExecutionTime) / TargetFunctionCallsCount;
        _dispersionExecutionTime += ((elapsedMs - prevMeanExecutionTime) * (elapsedMs - MeanExecutionTime) -
                                     _dispersionExecutionTime) / TargetFunctionCallsCount;
    }
}
