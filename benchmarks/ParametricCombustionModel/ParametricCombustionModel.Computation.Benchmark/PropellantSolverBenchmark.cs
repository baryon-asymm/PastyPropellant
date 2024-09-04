using BenchmarkDotNet.Attributes;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Test.Share.Helpers;

namespace ParametricCombustionModel.Computation.Benchmark;

public class PropellantSolverBenchmark
{
    private readonly ProblemContextByUnits _problemContext;
    private readonly double[] _vector = CombustionSolverParamsHelper.DefaultVector;

    private readonly ISolverVisitor _mixedPropellantSolver;
    private readonly ISolverVisitor _interPocketPropellantSolver;
    private readonly ISolverVisitor _pocketPropellantSolver;

    public PropellantSolverBenchmark()
    {
        var matrix = ProblemContextByUnitsMatrixBuilder.FromPropellants(PropellantHelper.GetPropellants())
                                                       .ForPressures(PressureHelper.GetPressures())
                                                       .BuildMatrix();
        _problemContext = matrix[0, 0];

        _mixedPropellantSolver = new MixedPropellantSolver();
        _interPocketPropellantSolver = new InterPocketPropellantSolver();
        _pocketPropellantSolver = new PocketPropellantSolver();
    }

    [Benchmark]
    public void CombustionSolverParamsFromVector()
    {
        _ = CombustionSolverParams.FromVector(_vector);
    }

    [Benchmark]
    public void MixedPropellantSolver()
    {
        var solverParams = CombustionSolverParams.FromVector(_vector);
        _problemContext.Accept(solverParams, _mixedPropellantSolver);
    }

    [Benchmark]
    public void InterPocketPropellantSolver()
    {
        var solverParams = CombustionSolverParams.FromVector(_vector);
        _problemContext.Accept(solverParams, _interPocketPropellantSolver);
    }

    [Benchmark]
    public void PocketPropellantSolver()
    {
        var solverParams = CombustionSolverParams.FromVector(_vector);
        _problemContext.Accept(solverParams, _pocketPropellantSolver);
    }
}
