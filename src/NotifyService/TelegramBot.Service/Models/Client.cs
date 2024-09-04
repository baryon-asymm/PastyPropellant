using Newtonsoft.Json;

namespace TelegramBot.Service.Models;

public record Client(
    [property: JsonProperty("token")]
    [property: JsonRequired]
    Guid Token,
    [property: JsonProperty("name")]
    [property: JsonRequired]
    string Name
);
