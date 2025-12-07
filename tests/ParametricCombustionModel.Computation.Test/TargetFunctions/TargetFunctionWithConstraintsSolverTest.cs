using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Computation.Test.Models;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Optimization.TargetFunctions;
using Xunit;

namespace ParametricCombustionModel.Computation.Test.TargetFunctions;

public class TargetFunctionWithConstraintsSolverTest
{
    [Fact]
    public void TargetFunctionValueTest()
    {
        var context = TestPropellant.GetOptimizationContext();
        var mixedPropellantSolvers = MixedPropellantSolversBuilder.FromPropellants(context.Propellants)
                                                                  .ForPressures(context.Pressures)
                                                                  .Build();
        var experimentalBurningRates = context.Propellants.GetExperimentalBurningRates(context.Pressures);
        var solver = new TargetFunctionNonlconSolver(experimentalBurningRates, mixedPropellantSolvers, (599, 751));

        var fi = new double[3];
        solver.RunTargetFunction(context.InitialPoint.ToArray(), fi);

        Assert.Equal(0.000198, Math.Round(fi[0], 6));
        Assert.Equal(748.965312, Math.Round(fi[1], 6));
        Assert.Equal(686.278355, Math.Round(fi[2], 6));
    }
}
