namespace ParametricCombustionModel.ReportMaking.Models;

/// <summary>
/// One titled block of the report's run-configuration summary: a heading and an ordered list of
/// label/value lines.
///
/// <para>The summary is passed in as pre-formatted text rather than as the host's configuration
/// objects on purpose. The report library must not take a dependency on the console host that owns the
/// configuration schema (the host already references this library), and the alternative — teaching this
/// library to read the schema — would mean every new setting had to be added in two places to appear in
/// the PDF. With this shape the host formats what it resolved and the report prints it verbatim.</para>
/// </summary>
/// <param name="Title">Section heading, e.g. "Search".</param>
/// <param name="Entries">Label/value lines, in display order.</param>
public sealed record RunConfigurationSection(string Title, IReadOnlyList<RunConfigurationEntry> Entries);

/// <summary>One label/value line of a <see cref="RunConfigurationSection"/>.</summary>
/// <param name="Label">Setting name as it should read to a human.</param>
/// <param name="Value">Resolved value, already formatted.</param>
public sealed record RunConfigurationEntry(string Label, string Value);
