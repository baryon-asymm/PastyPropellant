using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class CombustionSolverParamsReport : BaseReport, ITransformable<Queue<IPdfOperation>>
{
    public CombustionSolverParamsReport(
        OptimizationResult optimizationResult) : base(optimizationResult)
    {
    }

    public Queue<IPdfOperation> Transform()
    {
        var solverParams = Result.BestSolverParamsByUnits;
        var operations = new Queue<IPdfOperation>();

        operations.Enqueue(new PrintTextOperation(
                               CombustionSolverParamsReportResources.HeaderOfCombustionSolverParams,
                               TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ADecompose, solverParams.ADecompose),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EDecompose, solverParams.EDecompose),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.AKineticFlameInterPocket,
                                             solverParams.AKineticFlameInterPocket),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EKineticFlameInterPocket,
                                             solverParams.EKineticFlameInterPocket),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.AKineticFlamePocketOutSkeleton,
                                             solverParams.AKineticFlamePocketOutSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EKineticFlamePocketOutSkeleton,
                                             solverParams.EKineticFlamePocketOutSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.AKineticFlamePocketSkeleton,
                                             solverParams.AKineticFlamePocketSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EKineticFlamePocketSkeleton,
                                             solverParams.EKineticFlamePocketSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               "Order of the chemical reactions at the inter pocket region " + solverParams.NuInterPocket,
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               "Order of the chemical reactions at the pocket skeleton region " + solverParams.NuPocketSkeleton,
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               "Order of the chemical reactions at the pocket out skeleton region " + solverParams.NuPocketOutSkeleton,
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.HMetalBurning,
                                             solverParams.HMetalBurning),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EMetalBurning,
                                             solverParams.EMetalBurning),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.DeltaH, solverParams.DeltaH),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.KDiffusionHeight,
                                             solverParams.KDiffusionHeight),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
                               "Coefficient of the metal burning temperature " + solverParams.KMetalTemperature,
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());

        return operations;
    }
}
