using System.Text;
using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Resources;

namespace ParametricCombustionModel.ReportMaking.Reports.Text;

public class CombustionSolverParamsReport : BaseReport, ITransformable<string>
{
    public CombustionSolverParamsReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public string Transform()
    {
        var solverParams = Result.BestSolverParamsByUnits;
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine(CombustionSolverParamsReportResources.HeaderOfCombustionSolverParams);
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.ADecompose, solverParams.ADecompose);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.EDecompose, solverParams.EDecompose);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.AKineticFlameInterPocket,
                                   solverParams.AKineticFlameInterPocket);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.EKineticFlameInterPocket,
                                   solverParams.EKineticFlameInterPocket);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.AKineticFlamePocketOutSkeleton,
                                   solverParams.AKineticFlamePocketOutSkeleton);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.EKineticFlamePocketOutSkeleton,
                                   solverParams.EKineticFlamePocketOutSkeleton);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.AKineticFlamePocketSkeleton,
                                   solverParams.AKineticFlamePocketSkeleton);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.EKineticFlamePocketSkeleton,
                                   solverParams.EKineticFlamePocketSkeleton);
        stringBuilder.AppendLine();
        //stringBuilder.AppendFormat(CombustionSolverParamsReportResources.Nu, solverParams.Nu);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.HMetalBurning, solverParams.HMetalBurning);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.EMetalBurning, solverParams.EMetalBurning);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.DeltaH, solverParams.DeltaH);
        stringBuilder.AppendLine();
        stringBuilder.AppendFormat(CombustionSolverParamsReportResources.KDiffusionHeight,
                                   solverParams.KDiffusionHeight);
        stringBuilder.AppendLine();

        return stringBuilder.ToString();
    }
}
