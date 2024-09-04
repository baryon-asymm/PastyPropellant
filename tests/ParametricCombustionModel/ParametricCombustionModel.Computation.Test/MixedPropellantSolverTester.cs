using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Test.Share.Helpers;

namespace ParametricCombustionModel.Computation.Test;

public class MixedPropellantSolverTester
{
    [Fact]
    public void TestProblemContextByUnitsCase()
    {
        var propellants = PropellantHelper.GetPropellants();
        var pressures = PressureHelper.GetPressures();
        var solver = new MixedPropellantSolver();
        var solverParams = CombustionSolverParamsHelper.GetDefaultCombustionSolverParams();

        var matrixOfProblemContext = ProblemContextByUnitsMatrixBuilder.FromPropellants(propellants)
                                                                       .ForPressures(pressures)
                                                                       .BuildMatrix();
    }
}
