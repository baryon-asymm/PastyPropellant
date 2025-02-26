using System.Text;
using ParametricCombustionModel.Computation.Models.ProblemContexts;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Resources;
using UnitsNet;

namespace ParametricCombustionModel.ReportMaking.Reports.Text;

public class ParametricConstraintReport : BaseReport, ITransformable<string>
{
    public ParametricConstraintReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public string Transform()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine(ParametricConstraintReportResources.HeaderOfParametricConstraint);
        stringBuilder.AppendLine(ParametricConstraintReportResources.SurfaceTemperatureRangeConstraint);
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ParametricConstraintReportResources.MinSurfaceTemperature,
                                   GetMinSurfaceTemperature(Result.OptimizedContext.ProblemContextMatrix[0, 0]));
        stringBuilder.AppendLine();
        stringBuilder.Append('\t');
        stringBuilder.AppendFormat(ParametricConstraintReportResources.MaxSurfaceTemperature,
                                   GetMaxSurfaceTemperature(Result.OptimizedContext.ProblemContextMatrix[0, 0]));
        stringBuilder.AppendLine();

        return stringBuilder.ToString();
    }

    private static Temperature GetMinSurfaceTemperature(
        ProblemContextByUnits problemContext)
    {
        return problemContext.MinSurfaceTemperature;
    }

    private static Temperature GetMaxSurfaceTemperature(
        ProblemContextByUnits problemContext)
    {
        return problemContext.MaxSurfaceTemperature;
    }
}
