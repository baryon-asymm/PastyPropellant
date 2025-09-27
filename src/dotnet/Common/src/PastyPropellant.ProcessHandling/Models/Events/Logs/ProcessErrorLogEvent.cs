namespace PastyPropellant.ProcessHandling.Models.Events.Logs;

public record struct ProcessErrorLogEvent(
    string Message,
    string ProcessSender
);
