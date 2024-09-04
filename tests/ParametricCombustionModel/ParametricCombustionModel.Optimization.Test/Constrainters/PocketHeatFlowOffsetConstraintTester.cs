using ParametricCombustionModel.Optimization.Constrainers;
using ParametricCombustionModel.Optimization.Test.Models;
using ParametricCombustionModel.Optimization.Test.TargetFunctionSolvers;
using ParametricCombustionModel.Test.Common.Models;

namespace ParametricCombustionModel.Optimization.Test.Constrainters;

public class PocketHeatFlowOffsetConstraintTester
{
    [Fact]
    public void TestSampleNotCrossedConstraint()
    {
        const double heatFlowsSegmentSize = 100;

        var point = DefaultPointsHolder.ExistSolution;

        var constrainter = GetConstrainter(heatFlowsSegmentSize);
        var result = constrainter.GetPenaltyValue(BurningParams.ToVector(point), out _);

        Assert.Equal(PocketHeatFlowOffsetConstrainer.Passed, result);
    }

    [Fact]
    public void TestSampleCrossedConstraint()
    {
        const double heatFlowsSegmentSize = 10;
        const int precision = 6;
        const double expectedHeatFlowsSegmentSize = 96.223261;

        var point = DefaultPointsHolder.ExistSolution;

        var constrainter = GetConstrainter(heatFlowsSegmentSize);
        var result = constrainter.GetPenaltyValue(BurningParams.ToVector(point), out var currentHeatFlowsSegmentSize);

        Assert.Equal(PocketHeatFlowOffsetConstrainer.NoPassed, result);
        Assert.Equal(expectedHeatFlowsSegmentSize, Math.Round(currentHeatFlowsSegmentSize, precision));
    }

    public static PocketHeatFlowOffsetConstrainer GetConstrainter(double heatFlowsSegmentSize)
    {
        var solversMatrix = DefaultMatrixesHolder.SolversMatrix;
        var targetFunctionSolver = TargetFunctionSolverTester.GetSolver();

        return new(solversMatrix, targetFunctionSolver, heatFlowsSegmentSize);
    }
}
