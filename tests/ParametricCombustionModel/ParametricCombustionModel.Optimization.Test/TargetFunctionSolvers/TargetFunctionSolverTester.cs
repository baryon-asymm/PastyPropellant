using ParametricCombustionModel.Optimization.TargetFunctions;
using ParametricCombustionModel.Optimization.Test.Models;
using ParametricCombustionModel.Test.Common.Models;

namespace ParametricCombustionModel.Optimization.Test.TargetFunctionSolvers;

public class TargetFunctionSolverTester
{
    [Fact]
    public void TestExistSolution()
    {
        const int precision = 6;
        const double expectedTargetFunctionValue = 9.455265;

        var solver = GetSolver();

        var point = DefaultPointsHolder.ExistSolution;
        const int paramsCount = 3;
        Span<double> values = stackalloc double[paramsCount];

        solver.RunTargetFunction(BurningParams.ToVector(point), values);

        Assert.Equal(expectedTargetFunctionValue, Math.Round(values[0], precision));
    }

    [Fact]
    public void TestNotExistSolution()
    {
        const double expectedTargetFunctionValue = double.MaxValue;

        var solver = GetSolver();

        var point = DefaultPointsHolder.NotExistSolution;
        const int paramsCount = 3;
        Span<double> values = stackalloc double[paramsCount];

        solver.RunTargetFunction(BurningParams.ToVector(point), values);

        Assert.Equal(expectedTargetFunctionValue, values[0]);
    }

    public static TargetFunctionSolver GetSolver()
    {
        return new TargetFunctionSolver(DefaultMatrixesHolder.ExperimentalBurningRatesMatrix,
                                        DefaultMatrixesHolder.SolversMatrix);
    }
}
