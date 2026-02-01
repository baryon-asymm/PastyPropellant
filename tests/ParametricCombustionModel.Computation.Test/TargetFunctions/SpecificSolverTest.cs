using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Test.Models;
using ParametricCombustionModel.Computation.Utils;
using ParametricCombustionModel.Optimization.TargetFunctions;
using ParametricCombustionModel.Reporting.Models.ReportMaking;
using ParametricCombustionModel.Reporting.ReportMakers;
using Xunit;

namespace ParametricCombustionModel.Computation.Test.TargetFunctions;

public class SpecificSolverTest
{
    [Fact]
    public void SpecificBurningParamsTest()
    {
        var context = TestPropellant.GetOptimizationContext();
        var mixedPropellantSolvers = MixedPropellantSolversBuilder.FromPropellants(context.Propellants)
                                                                  .ForPressures(context.Pressures)
                                                                  .Build();
        var experimentalBurningRates = context.Propellants.GetExperimentalBurningRates(context.Pressures);
        var solver = new TargetFunctionNonlconSolver(experimentalBurningRates, mixedPropellantSolvers, (600, 750));

        var fi = new double[3];
        solver.RunTargetFunction(context.InitialPoint.ToArray(), fi);

        var reportMaker = new ReportMaker();
        var reportContext = new ReportMakingContext(context.Pressures.ToArray().AsReadOnly(),
                                                    context.InitialPoint.ToArray().AsReadOnly(),
                                                    context.Propellants.ToArray().AsReadOnly());
        var reportOperationResult = reportMaker.GetReport(reportContext);
        if (reportOperationResult.IsSuccess)
        {
            var report = reportOperationResult.Value;
            var reportPrinter = new ParametricModelReportMaker();
            var reportStr = reportPrinter.MakeReport(context.InitialPoint, report);
            File.WriteAllText("report.txt", reportStr);
        }
    }
}
