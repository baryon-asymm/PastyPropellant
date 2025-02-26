using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Optimization.Models;

public class OptimizationResult
{
#region Fields

    private readonly Memory<double> _bestParams;

#endregion

#region Properties

    public CombustionSolverParamsByUnits BestSolverParamsByUnits => CombustionSolverParamsByUnits.FromVector(_bestParams.Span);
    public Span<double> BestSolverParamsBySpan => _bestParams.Span;
    public OptimizationProblemContextByUnits OptimizedContext { get; init; }

#endregion

#region Constructors

    public OptimizationResult(
        Memory<double> bestParams,
        OptimizationProblemContextByUnits context)
    {
        _bestParams = bestParams;
        OptimizedContext = context;
    }

#endregion
}
