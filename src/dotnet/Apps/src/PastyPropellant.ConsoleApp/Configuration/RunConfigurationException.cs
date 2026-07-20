namespace PastyPropellant.ConsoleApp.Configuration;

/// <summary>
/// A run-configuration file is unreadable, malformed, or would produce an invalid run.
///
/// <para>This is always fatal. A configuration that cannot be honoured must stop the host at startup
/// rather than fall back to defaults: a run that silently ignored half of its configuration would
/// produce results attributed to settings it never used, which is exactly the provenance failure the
/// resolved-run sidecar exists to prevent.</para>
/// </summary>
public sealed class RunConfigurationException : Exception
{
    /// <summary>Creates the exception with a message describing the offending setting.</summary>
    public RunConfigurationException(string message) : base(message) { }

    /// <summary>Creates the exception wrapping the underlying parse or validation failure.</summary>
    public RunConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
