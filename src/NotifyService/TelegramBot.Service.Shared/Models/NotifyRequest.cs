using System.Text.Json.Serialization;

namespace TelegramBot.Service.Shared.Models;

public record NotifyRequest(
    [property: JsonPropertyName("token")]
    [property: JsonRequired]
    Guid Token,
    [property: JsonPropertyName("message")]
    [property: JsonRequired]
    string Message
);
