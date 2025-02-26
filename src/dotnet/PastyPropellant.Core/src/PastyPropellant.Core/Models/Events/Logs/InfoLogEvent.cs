namespace PastyPropellant.Core.Models.Events.Logs;

public record struct InfoLogEvent(
    string Message,
    string? Sender = null
);
