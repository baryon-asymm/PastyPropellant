using MatlabGlobalSearch.Api;
using MatlabGlobalSearch.Api.DTOs;
using MatlabGlobalSearch.Api.Interfaces;

namespace MATLAB.Api.Test.ParallelGlobalSearch;

public class SampleParallelOptimizationProblemTest
{
    private static readonly IGlobalSearchOptimizer _optimizer = new ParallelGlobalSearchAdapter();

    private static readonly double[] _initialPoint = [-78.0, -78.0];
    private static readonly double[] _lowerBound = [-1000.0, -1000.0];
    private static readonly double[] _upperBound = [1000.0, 1000.0];

    private double RosenbrockTargetFunction(double[] point, double a, double b)
    {
        var x = point[0];
        var y = point[1];

        return Math.Pow(a - x, 2) + b * Math.Pow(y - x * x, 2);
    }

    private double NonlconFunctionPassEveryIter(double[] point)
    {
        return -1;
    }

    private bool OutputFcnNoneStop(double[]? args, string state)
    {
        return false;
    }

    [Fact]
    public async Task TestOptimizationWithOutputFcnAsync()
    {
        var context = new OptimizerSettings(_lowerBound,
                                            _upperBound,
                                            x => RosenbrockTargetFunction(x, 1, 100),
                                            _initialPoint,
                                            NonlconFunctionPassEveryIter,
                                            OutputFcnNoneStop,
                                            ParallelProbePoints: 1e2);
        var result = await _optimizer.RunAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.FinalPoint.Count());
        Assert.Equal(0.000, Math.Round(result.Value.TargetFunctionValue, 3));
        Assert.Equal(1.000, Math.Round(result.Value.FinalPoint.First(), 3));
        Assert.Equal(1.000, Math.Round(result.Value.FinalPoint.Last(), 3));
    }
}
