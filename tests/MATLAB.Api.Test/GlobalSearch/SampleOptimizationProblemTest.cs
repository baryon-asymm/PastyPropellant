using MatlabGlobalSearch.Api;
using MatlabGlobalSearch.Api.DTOs;
using MatlabGlobalSearch.Api.Interfaces;
using PastyPropellant.Core.Utils;

namespace MATLAB.Api.Test.GlobalSearch;

public class SampleOptimizationProblemTest
{
    private static readonly IGlobalSearchOptimizer _optimizer = new GlobalSearchAdapter();

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
                                            OutputFcnNoneStop);
        var result = await _optimizer.RunAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.FinalPoint.Count());
        Assert.Equal(0.000, Math.Round(result.Value.TargetFunctionValue, 3));
        Assert.Equal(1.000, Math.Round(result.Value.FinalPoint.First(), 3));
        Assert.Equal(1.000, Math.Round(result.Value.FinalPoint.Last(), 3));
    }

    [Fact]
    public async Task TestOptimizationWithoutOutputFcnAsync()
    {
        var context = new OptimizerSettings(_lowerBound,
                                            _upperBound,
                                            x => RosenbrockTargetFunction(x, 1, 100),
                                            _initialPoint,
                                            NonlconFunctionPassEveryIter);
        var result = await _optimizer.RunAsync(context);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.FinalPoint.Count());
        Assert.Equal(0.000, Math.Round(result.Value.TargetFunctionValue, 3));
        Assert.Equal(1.000, Math.Round(result.Value.FinalPoint.First(), 3));
        Assert.Equal(1.000, Math.Round(result.Value.FinalPoint.Last(), 3));
    }

    [Fact]
    public async Task TestOptimizationInParallelModeAsync()
    {
        var threadsCount = 12;
        var results = new OperationResult<OptimizerResult>[threadsCount];

        var firstContext = new OptimizerSettings(_lowerBound,
                                                 _upperBound,
                                                 x => RosenbrockTargetFunction(x, 1, 100),
                                                 _initialPoint,
                                                 NonlconFunctionPassEveryIter,
                                                 OutputFcnNoneStop);
        var secondContext = new OptimizerSettings(_lowerBound,
                                                  _upperBound,
                                                  x => RosenbrockTargetFunction(x, 100, 100),
                                                  _initialPoint,
                                                  OutputFcn: OutputFcnNoneStop);
        await Parallel.ForAsync(0,
                                threadsCount,
                                async (i, token) =>
                                {
                                    if (i % 2 == 0)
                                    {
                                        var result = await _optimizer.RunAsync(firstContext, token);
                                        results[i] = result;
                                    }
                                    else
                                    {
                                        var result = await _optimizer.RunAsync(secondContext, token);
                                        results[i] = result;
                                    }
                                });

        for (var i = 0; i < threadsCount; i++)
        {
            var result = results[i];

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.FinalPoint.Count());

            if (i % 2 == 0)
            {
                Assert.Equal(0.000, Math.Round(result.Value.TargetFunctionValue, 3));
                Assert.Equal(1.000, Math.Round(result.Value.FinalPoint.First(), 3));
                Assert.Equal(1.000, Math.Round(result.Value.FinalPoint.Last(), 3));
            }
            else
            {
                Assert.Equal(4675.0, Math.Round(result.Value.TargetFunctionValue));
                Assert.Equal(32.0, Math.Round(result.Value.FinalPoint.First()));
                Assert.Equal(1000.0, Math.Round(result.Value.FinalPoint.Last()));
            }
        }
    }
}
