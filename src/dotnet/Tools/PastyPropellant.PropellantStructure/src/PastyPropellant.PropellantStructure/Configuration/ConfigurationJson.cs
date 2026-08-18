using System.Text.Json;
using System.Text.Json.Serialization;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Configuration;

/// <summary>
/// The single JSON contract of this node — one options object for both directions,
/// so a file this port writes is a file this port reads.
/// </summary>
/// <remarks>
/// Quantities are written as bare numbers in SI, not as UnitsNet's own
/// {value, unit} shape. The reason is the trap the original already fell into: it
/// stored metres, printed micrometres, and guessed between them with a
/// <c>.ge. 0.1</c> heuristic that silently reads 0.05 µm as metres. A record whose
/// unit is fixed by its field name cannot be guessed at.
/// </remarks>
internal static class ConfigurationJson
{
    internal static JsonSerializerOptions Options { get; } = Build();

    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            WriteIndented = true,
            // A misspelt member is a setting that silently kept its default, which
            // is exactly the failure this node exists to prevent.
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            NumberHandling = JsonNumberHandling.Strict,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new LengthMetresConverter());
        options.Converters.Add(new DensityKilogramsPerCubicMetreConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    private sealed class LengthMetresConverter : JsonConverter<Length>
    {
        public override Length Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Length.FromMeters(reader.GetDouble());

        public override void Write(Utf8JsonWriter writer, Length value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.Meters);
    }

    private sealed class DensityKilogramsPerCubicMetreConverter : JsonConverter<Density>
    {
        public override Density Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Density.FromKilogramsPerCubicMeter(reader.GetDouble());

        public override void Write(Utf8JsonWriter writer, Density value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.KilogramsPerCubicMeter);
    }
}
