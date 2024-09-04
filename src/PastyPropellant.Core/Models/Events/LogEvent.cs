namespace PastyPropellant.Core.Models.Events;

public readonly struct LogEvent
{
    public ReadOnlyMemory<char> Message { get; init; }
}
