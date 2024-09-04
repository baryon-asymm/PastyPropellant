using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Computation.Test.Models;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Optimization.TargetFunctions;
using Xunit;

namespace ParametricCombustionModel.Computation.Test.TargetFunctions;

public class TargetFunctionSolverTest
{
    [Fact]
    public void TargetFunctionValueTest()
    {
        var context = TestPropellant.GetOptimizationContext();
        var mixedPropellantSolvers = MixedPropellantSolversBuilder.FromPropellants(context.Propellants)
                                                                  .ForPressures(context.Pressures)
                                                                  .Build();
        var experimentalBurningRates = context.Propellants.GetExperimentalBurningRates(context.Pressures);
        var solver = new TargetFunctionSolver(experimentalBurningRates, mixedPropellantSolvers);
        
        var fi = new double[1];
        solver.RunTargetFunction(context.InitialPoint.ToArray(), fi);

        Assert.Equal(0.000198, Math.Round(fi[0], 6));
    }

    [Fact]
    public void ReportMakingTest()
    {
        var context = TestPropellant.GetOptimizationContext();
        var mixedPropellantSolvers = MixedPropellantSolversBuilder.FromPropellants(context.Propellants)
                                                                  .ForPressures(context.Pressures)
                                                                  .Build();
        var experimentalBurningRates = context.Propellants.GetExperimentalBurningRates(context.Pressures);
        var solver = new TargetFunctionSolver(experimentalBurningRates, mixedPropellantSolvers);
    }
}
