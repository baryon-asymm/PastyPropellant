using BenchmarkDotNet.Attributes;
using ParametricCombustionModel.Computation.Builders;
using ParametricCombustionModel.Computation.Interfaces;
using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Computation.Solvers;
using ParametricCombustionModel.Test.Share.Helpers;

namespace ParametricCombustionModel.Computation.Benchmark;

/// <summary>
/// Benchmarks various propellant solvers to assess their performance using both <see cref="ProblemContextByUnits"/> and <see cref="ProblemContextByDoubles"/>.
/// </summary>
public class PropellantSolverBenchmark
{
    private readonly ProblemContextByUnits _problemContextByUnits;
    private readonly ProblemContextByDoubles _problemContextByDoubles;
    private readonly double[] _vector = CombustionSolverParamsHelper.DefaultVector;

    private readonly ISolverVisitor _mixedPropellantSolver;
    private readonly ISolverVisitor _interPocketPropellantSolver;
    private readonly ISolverVisitor _pocketPropellantSolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropellantSolverBenchmark"/> class.
    /// Sets up the necessary problem contexts and solvers for benchmarking.
    /// </summary>
    public PropellantSolverBenchmark()
    {
        var matrix = ProblemContextByUnitsMatrixBuilder.FromPropellants(PropellantHelper.GetPropellants())
                                                       .ForPressures(PressureHelper.GetPressures())
                                                       .BuildMatrix();
        _problemContextByUnits = matrix[0, 0];

        var matrixByDoubles = ProblemContextByDoublesMatrixBuilder.FromPropellants(PropellantHelper.GetPropellants())
                                                                  .ForPressures(PressureHelper.GetPressures())
                                                                  .BuildMatrix();
        _problemContextByDoubles = matrixByDoubles[0, 0];

        _mixedPropellantSolver = new MixedPropellantSolver();
        _interPocketPropellantSolver = new InterPocketPropellantSolver();
        _pocketPropellantSolver = new PocketPropellantSolver();
    }

    /// <summary>
    /// Benchmarks the creation of <see cref="CombustionSolverParamsByUnits"/> from a vector of doubles.
    /// </summary>
    [Benchmark]
    public void CombustionSolverParamsFromVector()
    {
        _ = CombustionSolverParamsByUnits.FromVector(_vector);
    }

#region Utilization of UnitsNet

    /// <summary>
    /// Benchmarks the <see cref="MixedPropellantSolver"/> using <see cref="ProblemContextByUnits"/>.
    /// </summary>
    [Benchmark]
    public void MixedPropellantSolverUnits()
    {
        var solverParams = CombustionSolverParamsByUnits.FromVector(_vector);
        _problemContextByUnits.Accept(solverParams, _mixedPropellantSolver);
    }

    /// <summary>
    /// Benchmarks the <see cref="InterPocketPropellantSolver"/> using <see cref="ProblemContextByUnits"/>.
    /// </summary>
    [Benchmark]
    public void InterPocketPropellantSolverUnits()
    {
        var solverParams = CombustionSolverParamsByUnits.FromVector(_vector);
        _problemContextByUnits.Accept(solverParams, _interPocketPropellantSolver);
    }

    /// <summary>
    /// Benchmarks the <see cref="PocketPropellantSolver"/> using <see cref="ProblemContextByUnits"/>.
    /// </summary>
    [Benchmark]
    public void PocketPropellantSolverUnits()
    {
        var solverParams = CombustionSolverParamsByUnits.FromVector(_vector);
        _problemContextByUnits.Accept(solverParams, _pocketPropellantSolver);
    }

#endregion

#region Utilization of Doubles

    /// <summary>
    /// Benchmarks the <see cref="MixedPropellantSolver"/> using <see cref="ProblemContextByDoubles"/>.
    /// </summary>
    [Benchmark]
    public void MixedPropellantSolverDoubles()
    {
        var solverParams = CombustionSolverParamsByDoubles.FromVector(_vector);
        _problemContextByDoubles.Accept(solverParams, _mixedPropellantSolver);
    }

    /// <summary>
    /// Benchmarks the <see cref="InterPocketPropellantSolver"/> using <see cref="ProblemContextByDoubles"/>.
    /// </summary>
    [Benchmark]
    public void InterPocketPropellantSolverDoubles()
    {
        var solverParams = CombustionSolverParamsByDoubles.FromVector(_vector);
        _problemContextByDoubles.Accept(solverParams, _interPocketPropellantSolver);
    }

    /// <summary>
    /// Benchmarks the <see cref="PocketPropellantSolver"/> using <see cref="ProblemContextByDoubles"/>.
    /// </summary>
    [Benchmark]
    public void PocketPropellantSolverDoubles()
    {
        var solverParams = CombustionSolverParamsByDoubles.FromVector(_vector);
        _problemContextByDoubles.Accept(solverParams, _pocketPropellantSolver);
    }

#endregion
}
