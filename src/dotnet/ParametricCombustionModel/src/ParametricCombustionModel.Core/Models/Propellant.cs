using System.Text.Json;
using System.Text.Json.Serialization;
using ParametricCombustionModel.Core.Models.GasPhases;
using ParametricCombustionModel.Core.Models.PropellantComponents;

namespace ParametricCombustionModel.Core.Models;

public class PropellantComponentJsonConverter : JsonConverter<IEnumerable<BaseComponent>>
{
    public override IEnumerable<BaseComponent>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var components = new List<BaseComponent>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;

            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();

            var propertyName = reader.GetString() ?? throw new JsonException();

            BaseComponent component;
            if (propertyName.Equals(nameof(CombustibleBinder)))
                component = JsonSerializer.Deserialize<CombustibleBinder>(ref reader, options) ?? throw new JsonException();
            else if (propertyName.Equals(nameof(Octogen)))
                component = JsonSerializer.Deserialize<Octogen>(ref reader, options) ?? throw new JsonException();
            else if (propertyName.Equals(nameof(AmmoniumPerchlorate)))
                component = JsonSerializer.Deserialize<AmmoniumPerchlorate>(ref reader, options) ?? throw new JsonException();
            else if (propertyName.Equals(nameof(Aluminum)))
                component = JsonSerializer.Deserialize<Aluminum>(ref reader, options) ?? throw new JsonException();
            else
                throw new JsonException();

            components.Add(component);
        }

        return components;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IEnumerable<BaseComponent> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var component in value)
        {
            var type = component.GetType();

            writer.WritePropertyName(type.Name);

            if (type == typeof(CombustibleBinder))
                JsonSerializer.Serialize(writer, component as CombustibleBinder, options);
            else if (type == typeof(Octogen))
                JsonSerializer.Serialize(writer, component as Octogen, options);
            else if (type == typeof(AmmoniumPerchlorate))
                JsonSerializer.Serialize(writer, component as AmmoniumPerchlorate, options);
            else if (type == typeof(Aluminum))
                JsonSerializer.Serialize(writer, component as Aluminum, options);
        }

        writer.WriteEndObject();
    }
}

public record ConfidenceInterval(
    [property: JsonPropertyName("x_value")]
    [property: JsonRequired]
    double XValue,
    [property: JsonPropertyName("y_value")]
    [property: JsonRequired]
    double YValue,
    [property: JsonPropertyName("size_of_confidence_interval")]
    [property: JsonRequired]
    double SizeOfConfidenceInterval
);

public record Propellant(
    [property: JsonPropertyName("name")]
    [property: JsonRequired]
    string Name,
    [property: JsonPropertyName("a")]
    [property: JsonRequired]
    double A,
    [property: JsonPropertyName("nu")]
    [property: JsonRequired]
    double Nu,
    [property: JsonPropertyName("density")]
    [property: JsonRequired]
    double Density,
    [property: JsonPropertyName("specific_heat_capacity")]
    [property: JsonRequired]
    double SpecificHeatCapacity,
    [property: JsonPropertyName("initial_temperature")]
    [property: JsonRequired]
    double InitialTemperature,
    [property: JsonPropertyName("pocket_surface_fraction_coefficients")]
    [property: JsonRequired]
    IEnumerable<double> PocketSurfaceFractionCoefficients,
    [property: JsonPropertyName("confidence_intervals")]
    IEnumerable<ConfidenceInterval>? ConfidenceIntervals,
    [property: JsonPropertyName("pocket_mass_fraction")]
    [property: JsonRequired]
    double PocketMassFraction,
    [property: JsonPropertyName("inter_pocket_gas_phase")]
    [property: JsonRequired]
    HomogeneousGasPhase InterPocketGasPhase,
    [property: JsonPropertyName("pocket_gas_phase")]
    [property: JsonRequired]
    HeterogeneousGasPhase PocketGasPhase,
    [property: JsonPropertyName("components")]
    [property: JsonRequired]
    [property: JsonConverter(typeof(PropellantComponentJsonConverter))]
    IEnumerable<BaseComponent> Components
);
