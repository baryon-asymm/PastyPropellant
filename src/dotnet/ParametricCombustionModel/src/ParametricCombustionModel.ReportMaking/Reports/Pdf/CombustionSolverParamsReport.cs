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

        // ADecompose - index 0
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ADecompose, solverParams.ADecompose),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[0]:E3}; {Result.UpperBound[0]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // EDecompose - index 1
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EDecompose, solverParams.EDecompose),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[1]:E3}; {Result.UpperBound[1]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // AKineticFlameInterPocket - index 2
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.AKineticFlameInterPocket,
                                             solverParams.AKineticFlameInterPocket),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[2]:E3}; {Result.UpperBound[2]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // EKineticFlameInterPocket - index 3
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EKineticFlameInterPocket,
                                             solverParams.EKineticFlameInterPocket),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[3]:E3}; {Result.UpperBound[3]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // AKineticFlamePocketOutSkeleton - index 4
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.AKineticFlamePocketOutSkeleton,
                                             solverParams.AKineticFlamePocketOutSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[4]:E3}; {Result.UpperBound[4]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // EKineticFlamePocketOutSkeleton - index 5
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EKineticFlamePocketOutSkeleton,
                                             solverParams.EKineticFlamePocketOutSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[5]:E3}; {Result.UpperBound[5]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // AKineticFlamePocketSkeleton - index 6
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.AKineticFlamePocketSkeleton,
                                             solverParams.AKineticFlamePocketSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[6]:E3}; {Result.UpperBound[6]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // EKineticFlamePocketSkeleton - index 7
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.EKineticFlamePocketSkeleton,
                                             solverParams.EKineticFlamePocketSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[7]:E3}; {Result.UpperBound[7]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // NuInterPocket - index 8
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.NuInterPocket,
                                             solverParams.NuInterPocket),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[8]:E3}; {Result.UpperBound[8]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // NuPocketOutSkeleton - index 9
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.NuPocketOutSkeleton,
                                             solverParams.NuPocketOutSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[9]:E3}; {Result.UpperBound[9]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // NuPocketSkeleton - index 10
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.NuPocketSkeleton,
                                             solverParams.NuPocketSkeleton),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[10]:E3}; {Result.UpperBound[10]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // AMetalBurningConstant - index 11
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.AMetalBurningConstant,
                                             solverParams.AMetalBurningConstant),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[11]:E3}; {Result.UpperBound[11]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // BMetalBurningConstant - index 12
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.BMetalBurningConstant,
                                             solverParams.BMetalBurningConstant),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[12]:E3}; {Result.UpperBound[12]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // DeltaH - index 13
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.DeltaH, solverParams.DeltaH),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[13]:E3}; {Result.UpperBound[13]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // KDiffusionHeight - index 14
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.KDiffusionHeight,
                                             solverParams.KDiffusionHeight),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[14]:E3}; {Result.UpperBound[14]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // APowOrder - index 15
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.APowOrder,
                                             solverParams.APowOrder),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[15]:E3}; {Result.UpperBound[15]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // BPowOrder - index 16
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.BPowOrder,
                                             solverParams.BPowOrder),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[16]:E3}; {Result.UpperBound[16]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        
        // KCoefficientRadiationTemperature - index 17
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.KCoefficientRadiationTemperature,
                                             solverParams.KCoefficientRadiationTemperature),
                               TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
                               string.Format(CombustionSolverParamsReportResources.ParameterBounds, 
                                             $"[{Result.LowerBound[17]:E3}; {Result.UpperBound[17]:E3}]"),
                               TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        return operations;
    }
}
