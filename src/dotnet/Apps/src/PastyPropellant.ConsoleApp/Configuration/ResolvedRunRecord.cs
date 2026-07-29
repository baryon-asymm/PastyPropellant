namespace PastyPropellant.ConsoleApp.Configuration;

/// <summary>
/// The provenance record of a single run: everything needed to say what configuration actually
/// produced the artefacts sitting next to it.
///
/// <para><b>Why this exists.</b> These values were originally hardcoded, and that had a real virtue —
/// every experiment variation needed a recompile, so every run was traceable to a commit. Making the
/// configuration a file gives that up unless the run records what it used, so this record is written
/// before the search starts and is summarised in the PDF report.</para>
///
/// <para><b>It records the effective values, not the requested ones.</b> <see cref="Configuration"/> is
/// the merged configuration after defaults are applied, and <see cref="Effective"/> carries the values
/// derived from it at run time — in particular the population size and the worker count <em>after</em>
/// the divisibility-reduction rule has run, and the expanded 32-element bound vectors actually handed
/// to the optimiser. A reader never has to re-derive anything to know what happened.</para>
/// </summary>
public sealed record ResolvedRunRecord
{
    /// <summary>Sidecar schema version; bump when the shape changes incompatibly.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Which host path produced this record: <c>optimization</c> or <c>forward-eval</c>.</summary>
    public required string Mode { get; init; }

    /// <summary>UTC timestamp of the run this record describes.</summary>
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Absolute path of the configuration file, or a note that the built-in defaults were used.</summary>
    public required string ConfigurationSource { get; init; }

    /// <summary>
    /// Working directory the run resolved its input file, configuration file and outputs against.
    /// Recorded because every path in this host is relative to it.
    /// </summary>
    public required string WorkingDirectory { get; init; }

    /// <summary>The merged, validated configuration.</summary>
    public required RunConfiguration Configuration { get; init; }

    /// <summary>Values derived from the configuration at run time.</summary>
    public required EffectiveRunValues Effective { get; init; }
}

/// <summary>
/// Run-time-derived values: what the optimiser was actually given, as opposed to what the
/// configuration asked for.
/// </summary>
public sealed record EffectiveRunValues
{
    /// <summary>Population size the strategy branch selected (dimension × the branch's multiplier).</summary>
    public required int PopulationSize { get; init; }

    /// <summary>
    /// In-process DE worker count after the reduction rule. For the fixed-population strategies the
    /// host starts at <see cref="MachineProcessorCount"/> − 1 and walks down until the count divides
    /// the population evenly, so this is machine-dependent and cannot be reconstructed from the
    /// configuration alone.
    /// </summary>
    public required int ProcessorsCount { get; init; }

    /// <summary><see cref="Environment.ProcessorCount"/> on the machine that ran this.</summary>
    public required int MachineProcessorCount { get; init; }

    /// <summary>Dimension of the vector handed to the optimiser (32 for the grouped formulation).</summary>
    public required int Dimensions { get; init; }

    /// <summary>
    /// RNG seed the search actually ran with, or null when it was left unseeded.
    ///
    /// <para>Only meaningful paired with <see cref="ProcessorsCount"/> immediately above: the library
    /// derives one random stream per worker, so replaying this seed at a different worker count produces a
    /// different search. The pair is what makes a run reproducible; either number alone does not.</para>
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>Human-readable description of the composed termination strategy.</summary>
    public required string TerminationStrategy { get; init; }

    /// <summary>Evaluation budget, set only for the budget-driven L-SHADE variant.</summary>
    public long? MaxEvaluationNumber { get; init; }

    /// <summary>L-SHADE p-best rate actually passed to the library; null for the fixed-population strategies.</summary>
    public double? PBestRate { get; init; }

    /// <summary>L-SHADE archive size rate actually passed; null for the fixed-population strategies.</summary>
    public double? ArchiveSizeRate { get; init; }

    /// <summary>L-SHADE memory size actually passed; null for the fixed-population strategies.</summary>
    public int? MemorySize { get; init; }

    /// <summary>The expanded 32-element lower bound the optimiser searched within.</summary>
    public required double[] GroupLowerBound { get; init; }

    /// <summary>The expanded 32-element upper bound the optimiser searched within.</summary>
    public required double[] GroupUpperBound { get; init; }
}
