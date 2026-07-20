using ParametricCombustionModel.ReportMaking.Enums;
using ParametricCombustionModel.ReportMaking.Interfaces;
using ParametricCombustionModel.ReportMaking.Models;
using ParametricCombustionModel.ReportMaking.PdfOperations;

namespace ParametricCombustionModel.ReportMaking.Reports.Pdf;

/// <summary>
/// Prints the fully-resolved run configuration: which configuration file (if any) was used, the search
/// settings, the penalty thresholds and the parameter bounds.
///
/// <para>This is the human-readable half of the run's provenance record; the machine-readable half is
/// the <c>run_configuration.resolved.json</c> sidecar, and both are produced from the same resolved
/// object. It exists because the settings it prints used to be compiled into the host, which made every
/// experiment traceable to a commit — an optional configuration file gives that up unless the report
/// states what the run actually used.</para>
///
/// <para>The values shown are the <em>effective</em> ones: population and worker count as the run
/// derived them, not as the file requested them.</para>
/// </summary>
public class RunConfigurationReport : ITransformable<Queue<IPdfOperation>>
{
    private readonly IReadOnlyList<RunConfigurationSection> _sections;

    /// <summary>Creates the report over the pre-formatted sections the host resolved.</summary>
    public RunConfigurationReport(IReadOnlyList<RunConfigurationSection> sections)
    {
        _sections = sections ?? throw new ArgumentNullException(nameof(sections));
    }

    /// <inheritdoc />
    public Queue<IPdfOperation> Transform()
    {
        var operations = new Queue<IPdfOperation>();

        operations.Enqueue(new PrintTextOperation(
            "Run Configuration (as resolved)", TextStyle.Bold | TextStyle.Underline));
        operations.Enqueue(new LineBreakOperation());

        foreach (var section in _sections)
        {
            operations.Enqueue(new PrintTextOperation(section.Title, TextStyle.Bold));
            operations.Enqueue(new LineBreakOperation());

            foreach (var entry in section.Entries)
            {
                operations.Enqueue(new AddTabOperation());
                operations.Enqueue(new PrintTextOperation($"{entry.Label}: ", TextStyle.Bold));
                operations.Enqueue(new PrintTextOperation(entry.Value, TextStyle.None));
                operations.Enqueue(new LineBreakOperation());
            }

            operations.Enqueue(new LineBreakOperation());
        }

        return operations;
    }
}
