using ParametricCombustionModel.Optimization.TargetFunctions;
using ParametricCombustionModel.Optimization.Test.Models;
using ParametricCombustionModel.Test.Common.Models;

namespace ParametricCombustionModel.Optimization.Test.TargetFunctionSolvers;

public class PenaltyTargetFunctionNonlconSolverTester
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
    public void TestPenaltySolution()
    {
        const int precision = 6;
        const double expectedTargetFunctionValue = 3217.212708;

        var solver = GetSolver();

        var point = DefaultPointsHolder.CrossedSurfaceTemperatureConstraintSolution;
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

    public static PenaltyTargetFunctionNonlconSolver GetSolver()
    {
        return new PenaltyTargetFunctionNonlconSolver(DefaultMatrixesHolder.ExperimentalBurningRatesMatrix,
                                                      DefaultMatrixesHolder.SolversMatrix,
                                                      (600, 750));
    }
}
