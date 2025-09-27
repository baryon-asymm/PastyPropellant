using ParametricCombustionModel.Computation.Models.KnownParams;

namespace ParametricCombustionModel.Optimization.Models;

public class OptimizationResult
{
#region Fields

    private readonly Memory<double> _lowerBound;
    private readonly Memory<double> _upperBound;

    private readonly Memory<double> _bestParams;

#endregion

#region Properties

    public CombustionSolverParamsByUnits BestSolverParamsByUnits => CombustionSolverParamsByUnits.FromVector(_bestParams.Span);

    public ReadOnlySpan<double> LowerBound => _lowerBound.Span;

    public ReadOnlySpan<double> UpperBound => _upperBound.Span;

    public Span<double> BestSolverParamsBySpan => _bestParams.Span;

    public OptimizationProblemByUnits OptimizedContext { get; init; }

#endregion

#region Constructors

    public OptimizationResult(
        Memory<double> lowerBound,
        Memory<double> upperBound,
        Memory<double> bestParams,
        OptimizationProblemByUnits context)
    {
        _lowerBound = lowerBound;
        _upperBound = upperBound;
        _bestParams = bestParams;
        OptimizedContext = context;
    }

#endregion
}
