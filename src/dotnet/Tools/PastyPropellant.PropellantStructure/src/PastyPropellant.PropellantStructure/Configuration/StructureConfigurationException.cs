namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// A configuration that cannot start a run. Thrown before anything is constructed,
/// never in the middle of a solve.
/// </summary>
public sealed class StructureConfigurationException : Exception
{
    /// <summary>Creates the exception with a single problem description.</summary>
    public StructureConfigurationException(string message)
        : base(message)
    {
        Problems = [message];
    }

    /// <summary>
    /// Creates the exception from every problem found, so one call reports the whole
    /// list rather than the first entry.
    /// </summary>
    public StructureConfigurationException(IReadOnlyList<string> problems)
        : base(Describe(problems))
    {
        Problems = problems;
    }

    /// <summary>Every problem found, in the order they were checked.</summary>
    public IReadOnlyList<string> Problems { get; }

    private static string Describe(IReadOnlyList<string> problems) =>
        problems.Count == 1
            ? problems[0]
            : $"The run configuration has {problems.Count} problems:"
              + Environment.NewLine
              + string.Join(Environment.NewLine, problems.Select(problem => "  - " + problem));
}
