namespace PastyPropellant.ProcessHandling.Models.Events.Logs;

public record struct ProcessInfoLogEvent(
    string Message,
    string ProcessSender
);
