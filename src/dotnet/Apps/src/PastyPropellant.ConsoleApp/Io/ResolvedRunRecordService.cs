using System.Text.Json;
using System.Text.Json.Serialization;
using PastyPropellant.ConsoleApp.Configuration;

namespace PastyPropellant.ConsoleApp.Io;

/// <summary>
/// Writes the machine-readable provenance sidecar, <c>run_configuration.resolved.json</c>, next to the
/// run's other artefacts in the working directory.
///
/// <para>This is the counterpart of <see cref="VectorFileService"/>: that file records <em>which point
/// in parameter space</em> a run produced, this one records <em>under what configuration</em> it was
/// produced. Together they make an archived output directory self-describing without reference to the
/// source tree — which is what the previously-hardcoded configuration achieved through recompilation.</para>
///
/// <para>It is written <b>before</b> the search starts, so a run that is killed, times out or crashes
/// still leaves behind a record of what it was attempting.</para>
/// </summary>
public static class ResolvedRunRecordService
{
    /// <summary>Sidecar file name, written to the process working directory.</summary>
    public const string FileName = "run_configuration.resolved.json";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Serialises <paramref name="record"/> to <see cref="FileName"/> and returns its path.</summary>
    public static string Write(ResolvedRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var path = Path.GetFullPath(FileName);
        File.WriteAllText(path, Serialize(record));

        return path;
    }

    /// <summary>Serialises <paramref name="record"/> to the sidecar's JSON representation.</summary>
    public static string Serialize(ResolvedRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return JsonSerializer.Serialize(record, WriteOptions);
    }
}
