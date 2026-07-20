namespace PastyPropellant.Core.Models;

/// <summary>
/// Single source of truth for the composition-group ordering used by the grouped 32-parameter
/// formulation. The index is the <c>compositionIndex</c> / <c>groupIndex</c> used everywhere
/// downstream — the raw 32-vector layout (<c>shared[0-10]</c>, <c>Bas_2[11-17]</c>,
/// <c>Bas_1[18-24]</c>, <c>Bas_0[25-31]</c>), <c>GroupCombustionSolverParams.ToCompositionVector</c>,
/// <c>GroupOptimizationResult.CompositionContexts</c>, and the merged matrix produced by
/// <c>GroupOptimizationResultAdapter</c>:
/// <list type="bullet">
/// <item>0 — the Bas_2 group, which also covers Bas_3 and Bas_4 (they share the same specific parameters)</item>
/// <item>1 — Bas_1</item>
/// <item>2 — Bas_0</item>
/// </list>
///
/// Two display forms exist and are deliberately kept distinct, because they were introduced
/// independently and their rendered text is part of already-published output:
/// <see cref="ReportNames"/> is the verbose form used in the generated PDF reports, and
/// <see cref="ConsoleNames"/> is the compact form used in the console host's progress output.
/// They must stay byte-identical to what each surface printed before they were centralised here;
/// pick the form matching the surface you are writing to rather than converging them.
///
/// Any regrouping of the compositions must change the ordering here once — that is the point of
/// this type, which replaced six copy-pasted literal arrays.
/// </summary>
public static class CompositionGroups
{
    /// <summary>
    /// Number of composition groups in the grouped formulation. The 32-vector layout and every
    /// per-group loop are sized by this.
    /// </summary>
    public const int Count = 3;

    /// <summary>
    /// Verbose composition names, in <c>compositionIndex</c> order, as rendered in the PDF reports.
    /// Spells out that the Bas_2 group subsumes Bas_3 and Bas_4, since a report reader has no other
    /// cue that only three names cover five fuels.
    /// </summary>
    public static IReadOnlyList<string> ReportNames { get; } =
        ["Bas_2 (includes Bas_3, Bas_4)", "Bas_1", "Bas_0"];

    /// <summary>
    /// Compact composition names, in <c>compositionIndex</c> order, as written to the console by the
    /// optimisation and forward-eval paths.
    /// </summary>
    public static IReadOnlyList<string> ConsoleNames { get; } =
        ["Bas_2+Bas_3+Bas_4", "Bas_1", "Bas_0"];
}
