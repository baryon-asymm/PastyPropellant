using System.Text.Json.Serialization;

namespace PastyPropellant.ConsoleApp.Models;

public record GlobalTicket(
    [property: JsonPropertyName("name")]
    [property: JsonRequired]
    string Name,
    [property: JsonPropertyName("parametric_model_optimization_tickets_file")]
    string? ParametricModelOptimizationTicketsFilePath,
    [property: JsonPropertyName("processor_count")]
    int? ProcessorCount,
    [property: JsonPropertyName("telegram_bot_settings_file")]
    string? TelegramBotSettingsFilePath
);
