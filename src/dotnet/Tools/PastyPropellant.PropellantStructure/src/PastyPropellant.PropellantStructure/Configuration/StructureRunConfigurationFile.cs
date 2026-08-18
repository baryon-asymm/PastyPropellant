using System.Text.Json;

namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// Reads a run configuration from JSON.
/// </summary>
/// <remarks>
/// Reading is separate from <see cref="StructureConfigurationValidator.Validate"/>:
/// this class answers "is this a configuration at all", the validator answers "can
/// it start a run". Both throw <see cref="StructureConfigurationException"/>, so a
/// caller that only wants to report bad input has one type to catch.
/// <para>
/// A member the type does not have is an error, not a note. The alternative — a
/// misspelt key quietly leaving its setting at the default — is the failure mode
/// this whole node exists to rule out.
/// </para>
/// </remarks>
public static class StructureRunConfigurationFile
{
    /// <summary>Reads the file at <paramref name="path"/>.</summary>
    /// <exception cref="StructureConfigurationException">
    /// the file is missing, is not JSON, is missing a required member, or carries a
    /// member the configuration does not have
    /// </exception>
    public static StructureRunConfiguration Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new StructureConfigurationException(
                $"The run configuration '{path}' could not be read: {exception.Message}");
        }

        try
        {
            return Parse(json);
        }
        catch (StructureConfigurationException exception)
        {
            throw new StructureConfigurationException($"{path}: {exception.Message}");
        }
    }

    /// <summary>Parses configuration JSON held in memory.</summary>
    /// <exception cref="StructureConfigurationException">as for <see cref="Read"/></exception>
    public static StructureRunConfiguration Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        StructureRunConfiguration? configuration;
        try
        {
            configuration = JsonSerializer.Deserialize<StructureRunConfiguration>(json, ConfigurationJson.Options);
        }
        catch (JsonException exception)
        {
            throw new StructureConfigurationException(exception.Message);
        }

        return configuration
            ?? throw new StructureConfigurationException(
                "The run configuration is JSON null; a configuration object was expected.");
    }
}
