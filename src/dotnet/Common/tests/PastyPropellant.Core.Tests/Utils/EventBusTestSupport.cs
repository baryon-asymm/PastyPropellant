namespace PastyPropellant.Core.Tests.Utils;

using PastyPropellant.Core.Utils;

/// <summary>
/// Test-only event payload. <see cref="EventBus{TEvent}"/> is a static, process-wide bus keyed by
/// the event <i>type</i>, so every test that gives itself a distinct <typeparamref name="TTag"/>
/// gets its own completely private bus. That is the primary isolation mechanism here: two tests
/// can never see each other's handlers even if one of them leaks a subscription or the runner
/// executes them concurrently.
/// </summary>
public readonly record struct BusEvent<TTag>(int Value);

/// <summary>Mutable counter box, incremented from handlers with <see cref="Interlocked"/>.</summary>
public sealed class Counter
{
    private int _value;

    public int Value => Volatile.Read(ref _value);

    public void Increment() => Interlocked.Increment(ref _value);
}

/// <summary>
/// Secondary isolation mechanism (belt and braces): tracks every handler a test subscribes and
/// unsubscribes all of them on <see cref="Dispose"/>, duplicates included, so nothing is left on
/// the static bus when the test finishes — even if the test body throws.
/// </summary>
public sealed class EventBusScope<TEvent> : IDisposable
    where TEvent : struct
{
    private readonly List<Action<TEvent>> _handlers = [];

    public Action<TEvent> Subscribe(Action<TEvent> handler)
    {
        EventBus<TEvent>.Subscribe(handler);

        lock (_handlers) _handlers.Add(handler);

        return handler;
    }

    public Action<TEvent> SubscribeCounter(Counter counter) => Subscribe(_ => counter.Increment());

    public void Unsubscribe(Action<TEvent> handler)
    {
        EventBus<TEvent>.Unsubscribe(handler);

        lock (_handlers) _handlers.Remove(handler);
    }

    public void Dispose()
    {
        lock (_handlers)
        {
            foreach (var handler in _handlers) EventBus<TEvent>.Unsubscribe(handler);

            _handlers.Clear();
        }
    }
}
