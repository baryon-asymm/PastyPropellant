using ParametricCombustionModel.Optimization.TargetFunctions;
using ParametricCombustionModel.Optimization.Test.Models;
using ParametricCombustionModel.Test.Common.Models;

namespace ParametricCombustionModel.Optimization.Test.TargetFunctionSolvers;

public class TargetFunctionNonlconSolverTester
{
    [Fact]
    public void TestExistSolution()
    {
        const int precision = 6;
        const double expectedTargetFunctionValue = 9.455265;

        var solver = GetSolver();

        var point = DefaultPointsHolder.ExistSolution;
        Span<double> values = stackalloc double[3];

        solver.RunTargetFunction(BurningParams.ToVector(point), values);

        Assert.Equal(expectedTargetFunctionValue, Math.Round(values[0], precision));
    }

    [Fact]
    public void TestNotExistSolution()
    {
        const double expectedTargetFunctionValue = double.MaxValue;
        const double expectedInterPocketSurfaceTemperature = -1;
        const double expectedPocketSurfaceTemperature = -1;

        var solver = GetSolver();

        var point = DefaultPointsHolder.NotExistSolution;
        Span<double> values = stackalloc double[3];

        solver.RunTargetFunction(BurningParams.ToVector(point), values);

        Assert.Equal(expectedTargetFunctionValue, values[0]);
        Assert.Equal(expectedInterPocketSurfaceTemperature, values[1]);
        Assert.Equal(expectedPocketSurfaceTemperature, values[2]);
    }

    [Fact]
    public void TestSurfaceTemperatureConstraint()
    {
        const int precision = 6;
        const double expectedTargetFunctionValue = double.MaxValue;
        const double expectedInterPocketSurfaceTemperature = 768.765445;
        const double expectedPocketSurfaceTemperature = 689.819472;

        var solver = GetSolver();

        var point = DefaultPointsHolder.CrossedSurfaceTemperatureConstraintSolution;
        Span<double> values = stackalloc double[3];

        solver.RunTargetFunction(BurningParams.ToVector(point), values);

        Assert.Equal(expectedTargetFunctionValue, values[0]);
        Assert.Equal(expectedInterPocketSurfaceTemperature, Math.Round(values[1], precision));
        Assert.Equal(expectedPocketSurfaceTemperature, Math.Round(values[2], precision));
    }

    public static TargetFunctionNonlconSolver GetSolver()
    {
        return new TargetFunctionNonlconSolver(DefaultMatrixesHolder.ExperimentalBurningRatesMatrix,
                                               DefaultMatrixesHolder.SolversMatrix,
                                               (600, 750));
    }
}
