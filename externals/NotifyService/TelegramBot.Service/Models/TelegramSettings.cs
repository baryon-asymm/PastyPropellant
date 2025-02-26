using Newtonsoft.Json;

namespace TelegramBot.Service.Models;

public record TelegramSettings(
    [property: JsonProperty("token")]
    [property: JsonRequired]
    string BotToken,
    [property: JsonProperty("chat_id")]
    [property: JsonRequired]
    string ChatId,
    [property: JsonProperty("timeout")]
    [property: JsonRequired]
    TimeSpan NotifyTimeout
);
