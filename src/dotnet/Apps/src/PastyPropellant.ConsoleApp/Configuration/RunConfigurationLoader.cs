using System.Text.Json;
using System.Text.Json.Serialization;

namespace PastyPropellant.ConsoleApp.Configuration;

/// <summary>
/// Locates, parses, merges and validates the optional run-configuration file.
///
/// <para><b>Optional by design.</b> With no file present the loader returns
/// <see cref="RunConfiguration.Default"/> — the values that used to be compiled into the host — so the
/// pre-configuration behaviour is not a fallback path but the literal default object. A file only
/// overrides members of that object.</para>
///
/// <para><b>Loud on every failure.</b> An explicitly requested file that does not exist, malformed
/// JSON, an unknown member name, an unknown parameter name in a bounds map, or any value that would
/// produce an invalid run all throw <see cref="RunConfigurationException"/> before a single object is
/// constructed. Nothing degrades to defaults once a file has been supplied.</para>
///
/// <para><b>Resolution.</b> Paths are resolved relative to the <em>process working directory</em>, the
/// same convention the propellants file and every output artefact already use, so a run directory
/// holds its inputs, its configuration and its outputs together.</para>
/// </summary>
public static class RunConfigurationLoader
{
    /// <summary>File the loader probes for when <c>--config</c> is not given.</summary>
    public const string DefaultFileName = "run-config.json";

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // A misspelled or obsolete setting must fail the run, not be silently dropped — otherwise the
        // sidecar would faithfully record a configuration the author did not think they were asking for.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Loads the configuration for this run.
    /// </summary>
    /// <param name="explicitPath">
    /// Path supplied via <c>--config</c>. When null the loader probes <see cref="DefaultFileName"/> in
    /// <paramref name="baseDirectory"/> and falls back to the built-in defaults if it is absent. When
    /// non-null the file is mandatory.
    /// </param>
    /// <param name="baseDirectory">
    /// Directory relative paths are resolved against. Defaults to the process working directory, which
    /// is the host's convention for the propellants file and every output artefact alike; tests pass an
    /// explicit directory so they never have to mutate process-wide state to exercise the probe.
    /// </param>
    /// <exception cref="RunConfigurationException">The file is missing, malformed or invalid.</exception>
    public static LoadedRunConfiguration Load(string? explicitPath = null, string? baseDirectory = null)
    {
        var root = baseDirectory ?? Directory.GetCurrentDirectory();

        if (explicitPath != null)
        {
            if (string.IsNullOrWhiteSpace(explicitPath))
                throw new RunConfigurationException("--config requires a path to a configuration file.");

            var requestedPath = Path.GetFullPath(explicitPath, root);
            if (!File.Exists(requestedPath))
                throw new RunConfigurationException(
                    $"Configuration file not found: {requestedPath}. " +
                    "Omit --config to run with the built-in defaults.");

            return ReadFile(requestedPath);
        }

        var defaultPath = Path.GetFullPath(DefaultFileName, root);
        if (!File.Exists(defaultPath))
        {
            var defaults = RunConfiguration.Default;
            defaults.Validate();

            return new LoadedRunConfiguration
            {
                Configuration = defaults,
                SourceDescription = $"built-in defaults (no {DefaultFileName} in {root})",
                FilePath = null
            };
        }

        return ReadFile(defaultPath);
    }

    /// <summary>Parses and merges a configuration file that is known to exist.</summary>
    private static LoadedRunConfiguration ReadFile(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new RunConfigurationException($"Could not read configuration file '{path}': {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RunConfigurationException($"Could not read configuration file '{path}': {ex.Message}", ex);
        }

        RunConfigurationFile? file;
        try
        {
            file = JsonSerializer.Deserialize<RunConfigurationFile>(json, ReadOptions);
        }
        catch (JsonException ex)
        {
            throw new RunConfigurationException($"Invalid configuration file '{path}': {ex.Message}", ex);
        }

        if (file == null)
            throw new RunConfigurationException($"Configuration file '{path}' is empty.");

        var configuration = file.ApplyTo(RunConfiguration.Default);
        configuration.Validate();

        return new LoadedRunConfiguration
        {
            Configuration = configuration,
            SourceDescription = path,
            FilePath = path
        };
    }
}

/// <summary>
/// The configuration a run will use, together with where it came from. The provenance string is
/// carried alongside the values because the resolved sidecar has to state not just <em>what</em> the
/// run used but <em>whether a file was involved at all</em>.
/// </summary>
public sealed record LoadedRunConfiguration
{
    /// <summary>The merged, validated configuration.</summary>
    public required RunConfiguration Configuration { get; init; }

    /// <summary>Human-readable provenance: an absolute file path, or a note that defaults were used.</summary>
    public required string SourceDescription { get; init; }

    /// <summary>Absolute path of the configuration file, or null when the built-in defaults were used.</summary>
    public string? FilePath { get; init; }
}
