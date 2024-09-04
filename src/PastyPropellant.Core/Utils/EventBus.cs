namespace PastyPropellant.Core.Utils;

public static class EventBus<TEvent>
{
    private static readonly List<Action<TEvent>> _handlers = new();

    public static void Subscribe(Action<TEvent> handler)
    {
        _handlers.Add(handler);
    }

    public static void Unsubscribe(Action<TEvent> handler)
    {
        _handlers.Remove(handler);
    }

    public static void Publish(TEvent message)
    {
        foreach (var handler in _handlers) handler(message);
    }
}
