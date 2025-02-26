using System.Text.Json.Serialization;

namespace TelegramBot.Api.DTOs;

public record TelegramBotSettings(
    [property: JsonPropertyName("token"), JsonRequired] string BotToken,
    [property: JsonPropertyName("chat_id"), JsonRequired] string ChatId,
    [property: JsonPropertyName("timeout"), JsonRequired] TimeSpan NotifyTimeout
);
