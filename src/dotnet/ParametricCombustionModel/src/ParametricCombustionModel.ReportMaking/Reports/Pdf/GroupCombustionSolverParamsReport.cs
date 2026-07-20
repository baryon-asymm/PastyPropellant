using ParametricCombustionModel.Computation.Models.KnownParams;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;
using ParametricCombustionModel.ReportMaking.Resources;
using PastyPropellant.Core.Models;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

public class GroupCombustionSolverParamsReport : ITransformable<Queue<IPdfOperation>>
{
    // A gene sitting within this relative slack of a bound is flagged railed: the optimiser pushed
    // it to the edge of its box, so the fit is likely bound-limited and the bound should be widened.
    // The tolerance is scaled by max(|bound|, 1) so O(1) angles and O(1e13) pre-exponentials are
    // judged on the same footing.
    private const double RailRelativeTolerance = 1e-3;

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

        // The values below are display-rounded and must NOT be copied for replay — the radiative-temperature
        // closure amplifies rounding. Use the full-precision Reproduction Vector block for --forward-eval.
        operations.Enqueue(new PrintTextOperation(
            "Values below are display-rounded — not for replay; use the Reproduction Vector block.",
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new PrintTextOperation(
            "Each parameter shows its bounds and a rail flag: [RAILED@lo]/[RAILED@hi] means the gene sits "
            + "within 0.1% of a bound (fit is bound-limited — consider widening); [interior] means it does not.",
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        // Shared parameters (indices 0-10)
        operations.Enqueue(new PrintTextOperation(
            "Shared Parameters (All Compositions)",
            TextStyle.Bold | TextStyle.Underline));
        operations.Enqueue(new LineBreakOperation());

        AddParam(operations, CombustionSolverParamsReportResources.AKineticFlameInterPocket,
            groupParams.AKineticFlameInterPocket, 0);
        AddParam(operations, CombustionSolverParamsReportResources.EKineticFlameInterPocket,
            groupParams.EKineticFlameInterPocket, 1);
        AddParam(operations, CombustionSolverParamsReportResources.AKineticFlamePocketOutSkeleton,
            groupParams.AKineticFlamePocketOutSkeleton, 2);
        AddParam(operations, CombustionSolverParamsReportResources.EKineticFlamePocketOutSkeleton,
            groupParams.EKineticFlamePocketOutSkeleton, 3);
        AddParam(operations, CombustionSolverParamsReportResources.NuInterPocket,
            groupParams.NuInterPocket, 4);
        AddParam(operations, CombustionSolverParamsReportResources.NuPocketOutSkeleton,
            groupParams.NuPocketOutSkeleton, 5);
        AddParam(operations, CombustionSolverParamsReportResources.DeltaH,
            groupParams.DeltaH, 6);
        AddParam(operations, CombustionSolverParamsReportResources.KDiffusionHeight,
            groupParams.KDiffusionHeight, 7);
        AddParam(operations, CombustionSolverParamsReportResources.APowOrder,
            groupParams.APowOrder, 8);
        AddParam(operations, CombustionSolverParamsReportResources.BPowOrder,
            groupParams.BPowOrder, 9);
        AddParam(operations, CombustionSolverParamsReportResources.KCoefficientRadiationTemperature,
            groupParams.KCoefficientRadiationTemperature, 10);
        operations.Enqueue(new LineBreakOperation());

        // Composition-specific parameters (Condensed phase)
        // Note: Order is inverted compared to optimizer internals (0=Bas_2, 1=Bas_1, 2=Bas_0)
        var compositionNames = CompositionGroups.ReportNames;
        int[] blockStarts = [11, 18, 25];

        for (int comp = 0; comp < CompositionGroups.Count; comp++)
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

        AddParam(operations, CombustionSolverParamsReportResources.ADecompose, aDecompose, baseIndex);
        AddParam(operations, CombustionSolverParamsReportResources.EDecompose, eDecompose, baseIndex + 1);
        AddParam(operations, CombustionSolverParamsReportResources.AKineticFlamePocketSkeleton, aFlame, baseIndex + 2);
        AddParam(operations, CombustionSolverParamsReportResources.EKineticFlamePocketSkeleton, eFlame, baseIndex + 3);
        AddParam(operations, CombustionSolverParamsReportResources.NuPocketSkeleton, nuPocket, baseIndex + 4);
        AddParam(operations, CombustionSolverParamsReportResources.AMetalBurningConstant, aMetal, baseIndex + 5);
        AddParam(operations, CombustionSolverParamsReportResources.BMetalBurningConstant, bMetal, baseIndex + 6);
    }

    /// <summary>
    /// Emits one parameter: its display-rounded value line, then an indented italic bounds line with a
    /// rail flag. <paramref name="valueFormat"/> is a resource format string taking the value as {0};
    /// <paramref name="index"/> selects the matching lower/upper bound from the result vector.
    /// </summary>
    private void AddParam(Queue<IPdfOperation> operations, string valueFormat, double value, int index)
    {
        operations.Enqueue(new PrintTextOperation(
            string.Format(valueFormat, value),
            TextStyle.None));
        operations.Enqueue(new LineBreakOperation());
        operations.Enqueue(new AddTabOperation());

        var lower = _groupResult.LowerBound[index];
        var upper = _groupResult.UpperBound[index];

        // Bounds via the (localised) resource; the rail flag is appended outside string.Format so it
        // stays language-independent and is unaffected by the resource's numeric format specifier.
        operations.Enqueue(new PrintTextOperation(
            string.Format(CombustionSolverParamsReportResources.ParameterBounds, $"[{lower:E3}; {upper:E3}]")
                + " " + RailFlag(value, lower, upper),
            TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());
    }

    private static string RailFlag(double value, double lower, double upper)
    {
        var lowerTolerance = RailRelativeTolerance * Math.Max(Math.Abs(lower), 1.0);
        var upperTolerance = RailRelativeTolerance * Math.Max(Math.Abs(upper), 1.0);

        if (value - lower <= lowerTolerance)
            return "[RAILED@lo]";
        if (upper - value <= upperTolerance)
            return "[RAILED@hi]";
        return "[interior]";
    }
}
