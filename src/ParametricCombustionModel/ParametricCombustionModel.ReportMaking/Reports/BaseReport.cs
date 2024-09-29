using ParametricCombustionModel.Optimization.Models;

namespace ParametricCombustionModel.ReportMaking.Reports;

public abstract class BaseReport
{
    protected readonly OptimizationResult Result;

    public BaseReport(
        OptimizationResult optimizationResult)
    {
        Result = optimizationResult ?? throw new ArgumentNullException(nameof(optimizationResult));
    }
}
