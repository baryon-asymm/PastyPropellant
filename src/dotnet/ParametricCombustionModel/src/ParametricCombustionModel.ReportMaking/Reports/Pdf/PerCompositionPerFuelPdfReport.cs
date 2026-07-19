using ParametricCombustionModel.Optimization.Models;
using ParametricCombustionModel.Optimization.Results;
using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.PdfOperations;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

/// <summary>
/// Shared skeleton for the group-report sections that walk the grouped result composition-by-composition and,
/// within each composition, fuel-by-fuel: a bold/underlined title, an italic explanatory preamble, then one
/// bold/underlined sub-section per composition containing that composition's per-fuel block.
///
/// Derived reports supply only the text that differs (<see cref="Title"/>, <see cref="Introduction"/>,
/// <see cref="CompositionSectionSuffix"/>) and the per-fuel body (<see cref="AppendFuel"/>); the optional
/// per-composition and end-of-report trailers exist because <see cref="BurnRateErrorReport"/> reconciles its
/// numbers up to the objective while the other sections stop at the per-fuel level.
///
/// Works on the grouped result directly so every derived section uses the same composition ordering as the
/// rest of the group report.
/// </summary>
public abstract class PerCompositionPerFuelPdfReport : ITransformable<Queue<IPdfOperation>>
{
    protected PerCompositionPerFuelPdfReport(
        GroupOptimizationResult groupResult)
    {
        GroupResult = groupResult ?? throw new ArgumentNullException(nameof(groupResult));
    }

    protected GroupOptimizationResult GroupResult { get; }

    /// <summary>Bold/underlined heading printed once at the top of the section.</summary>
    protected abstract string Title { get; }

    /// <summary>Italic preamble explaining how to read the numbers, printed once under the title.</summary>
    protected abstract string Introduction { get; }

    /// <summary>
    /// Trailing half of each composition's sub-heading, rendered as "{composition name} - {suffix}".
    /// </summary>
    protected abstract string CompositionSectionSuffix { get; }

    /// <summary>
    /// Composition display names, in the raw 32-vector layout order. Each derived report still declares its
    /// own array; consolidating them repo-wide is tracked separately (ARCH-8).
    /// </summary>
    protected abstract IReadOnlyList<string> CompositionNames { get; }

    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();

        operations.Enqueue(new PrintTextOperation(Title, TextStyle.Bold | TextStyle.Underline));
        operations.Enqueue(new LineBreakOperation());

        operations.Enqueue(new PrintTextOperation(Introduction, TextStyle.Italic));
        operations.Enqueue(new LineBreakOperation());

        for (var comp = 0; comp < CompositionNames.Count; comp++)
        {
            var context = GroupResult.CompositionContexts[comp];

            operations.Enqueue(new LineBreakOperation());
            operations.Enqueue(new PrintTextOperation(
                $"{CompositionNames[comp]} - {CompositionSectionSuffix}", TextStyle.Bold | TextStyle.Underline));
            operations.Enqueue(new LineBreakOperation());

            for (var fuel = 0; fuel < context.PropellantCount; fuel++)
            {
                AppendFuel(operations, context, fuel);
            }

            AppendCompositionFooter(operations, context, comp);
        }

        AppendReportFooter(operations);

        return operations;
    }

    /// <summary>Per-fuel body: the part that actually differs between the derived sections.</summary>
    protected abstract void AppendFuel(
        Queue<IPdfOperation> operations,
        OptimizationProblemByUnits context,
        int fuel);

    /// <summary>Optional trailer after a composition's fuels. Prints nothing by default.</summary>
    protected virtual void AppendCompositionFooter(
        Queue<IPdfOperation> operations,
        OptimizationProblemByUnits context,
        int compositionIndex)
    {
    }

    /// <summary>Optional trailer after the last composition. Prints nothing by default.</summary>
    protected virtual void AppendReportFooter(
        Queue<IPdfOperation> operations)
    {
    }
}
