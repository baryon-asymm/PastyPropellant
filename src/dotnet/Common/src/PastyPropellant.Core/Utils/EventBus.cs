namespace PastyPropellant.Core.Utils;

/// <summary>
/// Static, process-wide typed pub/sub. The static pattern is intentional: components publish
/// typed events and the host subscribes once during startup, so no bus instance has to be
/// threaded through the object graph.
/// </summary>
/// <remarks>
/// Thread-safety strategy: <b>copy-on-write</b>. The handler list is an immutable array swapped
/// atomically by Subscribe/Unsubscribe under a CAS loop, so <see cref="Publish"/> is entirely
/// lock-free — it reads the array reference once and iterates that snapshot.
///
/// Copy-on-write is chosen over snapshot-on-publish because the read/write ratio is extreme:
/// Publish runs on the hot path (once per subprocess stdout line, from many process-reader
/// threads, and from the DE worker threads via the termination strategy), while
/// Subscribe/Unsubscribe happen a handful of times over an entire run. Snapshotting inside
/// Publish would take a lock and allocate a copy per log line; copy-on-write pays that cost
/// only on the rare mutation and leaves Publish as a plain array walk.
///
/// Consequences callers can rely on:
/// <list type="bullet">
/// <item>Subscribing or unsubscribing while a Publish is in flight never throws — the publisher
/// is iterating its own snapshot, so the collection it walks cannot be modified.</item>
/// <item>No lock is held while handlers run, so a handler may itself Subscribe, Unsubscribe or
/// Publish without deadlocking or re-entrancy trouble.</item>
/// <item>A handler subscribed after a Publish has already read the snapshot will not observe
/// that in-flight event. This is the same "eventually visible" semantics the previous
/// implementation had, minus the corruption.</item>
/// </list>
/// </remarks>
public static class EventBus<TEvent>
    where TEvent : struct
{
    private static Action<TEvent>[] _handlers = Array.Empty<Action<TEvent>>();

    public static void Subscribe(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        while (true)
        {
            var current = Volatile.Read(ref _handlers);

            var updated = new Action<TEvent>[current.Length + 1];
            Array.Copy(current, updated, current.Length);
            updated[current.Length] = handler;

            if (ReferenceEquals(Interlocked.CompareExchange(ref _handlers, updated, current), current))
                return;
        }
    }

    public static void Unsubscribe(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        while (true)
        {
            var current = Volatile.Read(ref _handlers);

            // Matches List<T>.Remove semantics: drop the first equal handler only, and treat a
            // handler that is not subscribed as a no-op.
            var index = Array.IndexOf(current, handler);
            if (index < 0) return;

            var updated = new Action<TEvent>[current.Length - 1];
            Array.Copy(current, updated, index);
            Array.Copy(current, index + 1, updated, index, current.Length - index - 1);

            if (ReferenceEquals(Interlocked.CompareExchange(ref _handlers, updated, current), current))
                return;
        }
    }

    public static void Publish(TEvent message)
    {
        // Single volatile read pins the snapshot; the array is never mutated in place, so this
        // iteration is safe against concurrent Subscribe/Unsubscribe without any lock.
        var handlers = Volatile.Read(ref _handlers);

        foreach (var handler in handlers) handler(message);
    }
}
