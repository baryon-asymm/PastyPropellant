using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class GroupCombustionSolverParamsReport : ITransformable<Queue<IPdfOperation>>
{
    private readonly GroupOptimizationResult _groupResult;

    public GroupCombustionSolverParamsReport(GroupOptimizationResult groupResult)
    {
        _groupResult = groupResult ?? throw new ArgumentNullException(nameof(groupResult));
    }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();

        // Parse group parameters
        var groupParams = GroupCombustionSolverParamsByDoubles.FromVector(_groupResult.BestParams);

        operations.Enqueue(new PrintTextOperation(
            "Group Combustion Solver Parameters",
            TextStyle.Bold));
        operations.Enqueue(new LineBreakOperation());

        // Shared parameters (indices 0-10)
        operations.Enqueue(new PrintTextOperation(
            "Shared Parameters (All Compositions)",
            TextStyle.Bold | TextStyle.Underline));
        operations.Enqueue(new LineBreakOperation());

        // AKineticFlameInterPocket
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.AKineticFlameInterPocket,
                groupParams.AKineticFlameInterPocket),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[0]:E3}; {_groupResult.UpperBound[0]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // EKineticFlameInterPocket
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.EKineticFlameInterPocket,
                groupParams.EKineticFlameInterPocket),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[1]:E3}; {_groupResult.UpperBound[1]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // AKineticFlamePocketOutSkeleton
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.AKineticFlamePocketOutSkeleton,
                groupParams.AKineticFlamePocketOutSkeleton),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[2]:E3}; {_groupResult.UpperBound[2]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // EKineticFlamePocketOutSkeleton
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.EKineticFlamePocketOutSkeleton,
                groupParams.EKineticFlamePocketOutSkeleton),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[3]:E3}; {_groupResult.UpperBound[3]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // NuInterPocket
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.NuInterPocket,
                groupParams.NuInterPocket),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[4]:E3}; {_groupResult.UpperBound[4]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // NuPocketOutSkeleton
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.NuPocketOutSkeleton,
                groupParams.NuPocketOutSkeleton),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[5]:E3}; {_groupResult.UpperBound[5]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // DeltaH
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.DeltaH,
                groupParams.DeltaH),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[6]:E3}; {_groupResult.UpperBound[6]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // KDiffusionHeight
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.KDiffusionHeight,
                groupParams.KDiffusionHeight),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[7]:E3}; {_groupResult.UpperBound[7]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // APowOrder
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.APowOrder,
                groupParams.APowOrder),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[8]:E3}; {_groupResult.UpperBound[8]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // BPowOrder
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.BPowOrder,
                groupParams.BPowOrder),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[9]:E3}; {_groupResult.UpperBound[9]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // KCoefficientRadiationTemperature
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.KCoefficientRadiationTemperature,
                groupParams.KCoefficientRadiationTemperature),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[10]:E3}; {_groupResult.UpperBound[10]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new LineBreakOperation());

        // Composition-specific parameters (Condensed phase)
        string[] compositionNames = ["Bas_2 (includes Bas_3, Bas_4)", "Bas_1", "Bas_0"];
        int[] blockStarts = [11, 18, 25];

        for (int comp = 0; comp < 3; comp++)
        {
            operations.Enqueue(new PrintTextOperation(
                $"{compositionNames[comp]} - Condensed Phase Parameters",
                TextStyle.Bold | TextStyle.Underline));
            operations.Enqueue(new LineBreakOperation());

            int baseIndex = blockStarts[comp];
            AddCompositionParameters(operations, groupParams, comp, baseIndex);

            operations.Enqueue(new LineBreakOperation());
        }

        return operations;
    }

    private void AddCompositionParameters(Queue<IPdfOperation> operations, 
        GroupCombustionSolverParamsByDoubles groupParams, int compositionIndex, int baseIndex)
    {
        // Get composition-specific parameters
        var (aDecompose, eDecompose, aFlame, eFlame, nuPocket, aMetal, bMetal) = compositionIndex switch
        {
            0 => (groupParams.ADecomposeBas0, groupParams.EDecomposeBas0, 
                  groupParams.AKineticFlamePocketSkeletonBas0, groupParams.EKineticFlamePocketSkeletonBas0,
                  groupParams.NuPocketSkeletonBas0, groupParams.AMetalBurningConstantBas0, groupParams.BMetalBurningConstantBas0),
            1 => (groupParams.ADecomposeBas1, groupParams.EDecomposeBas1,
                  groupParams.AKineticFlamePocketSkeletonBas1, groupParams.EKineticFlamePocketSkeletonBas1,
                  groupParams.NuPocketSkeletonBas1, groupParams.AMetalBurningConstantBas1, groupParams.BMetalBurningConstantBas1),
            2 => (groupParams.ADecomposeBas2, groupParams.EDecomposeBas2,
                  groupParams.AKineticFlamePocketSkeletonBas2, groupParams.EKineticFlamePocketSkeletonBas2,
                  groupParams.NuPocketSkeletonBas2, groupParams.AMetalBurningConstantBas2, groupParams.BMetalBurningConstantBas2),
            _ => throw new ArgumentOutOfRangeException()
        };

        // ADecompose
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ADecompose, aDecompose),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[baseIndex]:E3}; {_groupResult.UpperBound[baseIndex]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // EDecompose
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.EDecompose, eDecompose),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[baseIndex + 1]:E3}; {_groupResult.UpperBound[baseIndex + 1]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // AKineticFlamePocketSkeleton
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.AKineticFlamePocketSkeleton, aFlame),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[baseIndex + 2]:E3}; {_groupResult.UpperBound[baseIndex + 2]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // EKineticFlamePocketSkeleton
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.EKineticFlamePocketSkeleton, eFlame),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[baseIndex + 3]:E3}; {_groupResult.UpperBound[baseIndex + 3]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // NuPocketSkeleton
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.NuPocketSkeleton, nuPocket),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[baseIndex + 4]:E3}; {_groupResult.UpperBound[baseIndex + 4]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // AMetalBurningConstant
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.AMetalBurningConstant, aMetal),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[baseIndex + 5]:E3}; {_groupResult.UpperBound[baseIndex + 5]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // BMetalBurningConstant
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.BMetalBurningConstant, bMetal),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds,
                $"[{_groupResult.LowerBound[baseIndex + 6]:E3}; {_groupResult.UpperBound[baseIndex + 6]:E3}]"),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
    }
}
