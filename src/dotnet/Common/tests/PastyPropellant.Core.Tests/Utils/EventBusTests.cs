namespace PastyPropellant.Core.Tests.Utils;

using System.Collections.Concurrent;
using PastyPropellant.Core.Utils;

/// <summary>
/// Behaviour contract of <see cref="EventBus{TEvent}"/> after the copy-on-write / CAS rewrite.
///
/// Isolation: every test uses its own <c>BusEvent&lt;TagXx&gt;</c> closed generic, which resolves to
/// a distinct static bus, and wraps its subscriptions in an <see cref="EventBusScope{TEvent}"/>
/// so they are removed even when an assertion fails mid-test.
/// </summary>
public sealed class EventBusTests
{
    // One private tag per test => one private static bus per test.
    private sealed class TagDelivery;
    private sealed class TagNoSubscribers;
    private sealed class TagUnsubscribe;
    private sealed class TagUnsubscribeUnknown;
    private sealed class TagDuplicate;
    private sealed class TagPayload;
    private sealed class TagSubscribeDuringPublish;
    private sealed class TagUnsubscribeDuringPublish;
    private sealed class TagSelfUnsubscribeDuringPublish;
    private sealed class TagReentrantPublish;
    private sealed class TagNullHandler;
    private sealed class TagConcurrentSubscribe;
    private sealed class TagConcurrentChurn;
    private sealed class TagScopeCleanup;

    [Fact]
    public void Publish_ReachesEverySubscriber()
    {
        using var scope = new EventBusScope<BusEvent<TagDelivery>>();

        var first = new Counter();
        var second = new Counter();

        scope.SubscribeCounter(first);
        scope.SubscribeCounter(second);

        EventBus<BusEvent<TagDelivery>>.Publish(new BusEvent<TagDelivery>(1));

        Assert.Equal(1, first.Value);
        Assert.Equal(1, second.Value);
    }

    [Fact]
    public void Publish_CarriesThePayload()
    {
        using var scope = new EventBusScope<BusEvent<TagPayload>>();

        var received = new List<int>();
        scope.Subscribe(e => received.Add(e.Value));

        EventBus<BusEvent<TagPayload>>.Publish(new BusEvent<TagPayload>(7));
        EventBus<BusEvent<TagPayload>>.Publish(new BusEvent<TagPayload>(42));

        Assert.Equal([7, 42], received);
    }

    [Fact]
    public void Publish_WithNoSubscribers_IsANoOp()
    {
        // The initial state of a never-touched bus is an empty array, not null.
        EventBus<BusEvent<TagNoSubscribers>>.Publish(new BusEvent<TagNoSubscribers>(1));
    }

    [Fact]
    public void Unsubscribe_StopsDelivery()
    {
        using var scope = new EventBusScope<BusEvent<TagUnsubscribe>>();

        var kept = new Counter();
        var dropped = new Counter();

        scope.SubscribeCounter(kept);
        var droppedHandler = scope.SubscribeCounter(dropped);

        EventBus<BusEvent<TagUnsubscribe>>.Publish(new BusEvent<TagUnsubscribe>(1));

        scope.Unsubscribe(droppedHandler);

        EventBus<BusEvent<TagUnsubscribe>>.Publish(new BusEvent<TagUnsubscribe>(2));

        Assert.Equal(2, kept.Value);
        Assert.Equal(1, dropped.Value);
    }

    [Fact]
    public void Unsubscribe_OfAHandlerThatWasNeverSubscribed_IsANoOp()
    {
        using var scope = new EventBusScope<BusEvent<TagUnsubscribeUnknown>>();

        var counter = new Counter();
        scope.SubscribeCounter(counter);

        EventBus<BusEvent<TagUnsubscribeUnknown>>.Unsubscribe(_ => { });

        EventBus<BusEvent<TagUnsubscribeUnknown>>.Publish(new BusEvent<TagUnsubscribeUnknown>(1));

        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public void Subscribe_Twice_DeliversTwice_AndUnsubscribeRemovesOnlyOne()
    {
        // Documents the List<T>.Remove semantics the implementation deliberately preserves.
        using var scope = new EventBusScope<BusEvent<TagDuplicate>>();

        var counter = new Counter();
        var handler = new Action<BusEvent<TagDuplicate>>(_ => counter.Increment());

        scope.Subscribe(handler);
        scope.Subscribe(handler);

        EventBus<BusEvent<TagDuplicate>>.Publish(new BusEvent<TagDuplicate>(1));
        Assert.Equal(2, counter.Value);

        scope.Unsubscribe(handler);

        EventBus<BusEvent<TagDuplicate>>.Publish(new BusEvent<TagDuplicate>(2));
        Assert.Equal(3, counter.Value);
    }

    [Fact]
    public void Subscribe_FromInsideAHandler_DoesNotCorruptTheInFlightPublish()
    {
        // The whole point of copy-on-write: the publisher walks its own snapshot, so mutating the
        // subscription list from a handler cannot throw or skip handlers.
        using var scope = new EventBusScope<BusEvent<TagSubscribeDuringPublish>>();

        var outer = new Counter();
        var alsoOuter = new Counter();
        var lateJoiner = new Counter();

        scope.Subscribe(_ =>
        {
            outer.Increment();
            scope.SubscribeCounter(lateJoiner);
        });

        scope.SubscribeCounter(alsoOuter);

        EventBus<BusEvent<TagSubscribeDuringPublish>>.Publish(new BusEvent<TagSubscribeDuringPublish>(1));

        Assert.Equal(1, outer.Value);
        // The handler registered after the snapshot was taken must be skipped for this event...
        Assert.Equal(0, lateJoiner.Value);
        // ...and the handler that was already in the snapshot must still have been reached.
        Assert.Equal(1, alsoOuter.Value);

        EventBus<BusEvent<TagSubscribeDuringPublish>>.Publish(new BusEvent<TagSubscribeDuringPublish>(2));

        // Second publish: the first handler subscribed a second late joiner, so 2 by now.
        Assert.Equal(2, outer.Value);
        Assert.Equal(2, alsoOuter.Value);
        Assert.True(lateJoiner.Value >= 1, "the late joiner must be visible to the next publish");
    }

    [Fact]
    public void Unsubscribe_FromInsideAHandler_DoesNotCorruptTheInFlightPublish()
    {
        using var scope = new EventBusScope<BusEvent<TagUnsubscribeDuringPublish>>();

        var first = new Counter();
        var middle = new Counter();
        var last = new Counter();

        Action<BusEvent<TagUnsubscribeDuringPublish>>? lastHandler = null;

        scope.Subscribe(_ =>
        {
            first.Increment();
            scope.Unsubscribe(lastHandler!);
        });

        scope.SubscribeCounter(middle);
        lastHandler = scope.SubscribeCounter(last);

        EventBus<BusEvent<TagUnsubscribeDuringPublish>>.Publish(new BusEvent<TagUnsubscribeDuringPublish>(1));

        // Snapshot semantics: handlers removed mid-publish still receive the in-flight event, and
        // the handlers after the removal point are NOT skipped.
        Assert.Equal(1, first.Value);
        Assert.Equal(1, middle.Value);
        Assert.Equal(1, last.Value);

        EventBus<BusEvent<TagUnsubscribeDuringPublish>>.Publish(new BusEvent<TagUnsubscribeDuringPublish>(2));

        Assert.Equal(2, first.Value);
        Assert.Equal(2, middle.Value);
        Assert.Equal(1, last.Value);
    }

    [Fact]
    public void Handler_ThatUnsubscribesItself_DoesNotSkipTheFollowingHandlers()
    {
        using var scope = new EventBusScope<BusEvent<TagSelfUnsubscribeDuringPublish>>();

        var self = new Counter();
        var following = new Counter();

        Action<BusEvent<TagSelfUnsubscribeDuringPublish>>? selfHandler = null;
        selfHandler = _ =>
        {
            self.Increment();
            scope.Unsubscribe(selfHandler!);
        };

        scope.Subscribe(selfHandler);
        scope.SubscribeCounter(following);

        EventBus<BusEvent<TagSelfUnsubscribeDuringPublish>>.Publish(new BusEvent<TagSelfUnsubscribeDuringPublish>(1));
        EventBus<BusEvent<TagSelfUnsubscribeDuringPublish>>.Publish(new BusEvent<TagSelfUnsubscribeDuringPublish>(2));

        Assert.Equal(1, self.Value);
        Assert.Equal(2, following.Value);
    }

    [Fact]
    public void Handler_MayPublishReentrantly_WithoutDeadlock()
    {
        // No lock is held while handlers run, so re-entrant Publish must simply work.
        using var scope = new EventBusScope<BusEvent<TagReentrantPublish>>();

        var depth = 0;
        var seen = new List<int>();

        scope.Subscribe(e =>
        {
            seen.Add(e.Value);

            if (depth++ < 3)
                EventBus<BusEvent<TagReentrantPublish>>.Publish(new BusEvent<TagReentrantPublish>(e.Value + 1));
        });

        EventBus<BusEvent<TagReentrantPublish>>.Publish(new BusEvent<TagReentrantPublish>(0));

        Assert.Equal([0, 1, 2, 3], seen);
    }

    [Fact]
    public void Subscribe_And_Unsubscribe_RejectNullHandlers()
    {
        Assert.Throws<ArgumentNullException>(
            () => EventBus<BusEvent<TagNullHandler>>.Subscribe(null!));

        Assert.Throws<ArgumentNullException>(
            () => EventBus<BusEvent<TagNullHandler>>.Unsubscribe(null!));
    }

    [Fact]
    public void ConcurrentSubscribe_LosesNoHandlers_AndConcurrentUnsubscribe_LeavesNone()
    {
        // The CAS retry loop is what this covers: a lost update would show up as a call count
        // below threads*perThread, and a double-add as a count above it.
        const int threads = 8;
        const int perThread = 150;

        using var scope = new EventBusScope<BusEvent<TagConcurrentSubscribe>>();

        var calls = new Counter();
        var handlers = new Action<BusEvent<TagConcurrentSubscribe>>[threads][];

        for (var t = 0; t < threads; t++)
        {
            var mine = new Action<BusEvent<TagConcurrentSubscribe>>[perThread];

            for (var i = 0; i < perThread; i++)
            {
                // Capture the index so every handler is a genuinely distinct delegate instance.
                // A non-capturing lambda would be cached by the compiler into ONE shared instance,
                // which would make the per-handler Unsubscribe below a much weaker assertion.
                var id = i;

                mine[i] = _ =>
                {
                    if (id < 0) throw new InvalidOperationException();

                    calls.Increment();
                };
            }

            handlers[t] = mine;

            // Guards the assumption above: distinct instances, so IndexOf really does have to find
            // the right one.
            Assert.Equal(perThread, mine.Distinct().Count());
        }

        // Dedicated threads (not the pool) so the start barrier cannot deadlock against a pool
        // that decides to run fewer workers than the barrier's participant count.
        RunInParallel(threads, t =>
        {
            foreach (var handler in handlers[t]) scope.Subscribe(handler);
        });

        EventBus<BusEvent<TagConcurrentSubscribe>>.Publish(new BusEvent<TagConcurrentSubscribe>(1));

        Assert.Equal(threads * perThread, calls.Value);

        RunInParallel(threads, t =>
        {
            foreach (var handler in handlers[t]) scope.Unsubscribe(handler);
        });

        EventBus<BusEvent<TagConcurrentSubscribe>>.Publish(new BusEvent<TagConcurrentSubscribe>(2));

        Assert.Equal(threads * perThread, calls.Value);
    }

    [Fact]
    public void ConcurrentChurn_WhilePublishing_NeverThrows_AndSettlesOnTheExpectedHandlerSet()
    {
        // Publishers hammer the bus while subscribers churn. A non-atomic swap would surface as an
        // exception on the publisher thread or as a surviving/vanished handler at the end.
        const int churnThreads = 6;
        const int roundsPerThread = 300;

        using var scope = new EventBusScope<BusEvent<TagConcurrentChurn>>();

        var resident = new Counter();
        scope.SubscribeCounter(resident);

        var stopPublishing = 0;
        var publishFailures = new ConcurrentQueue<Exception>();

        var publishers = new Thread[2];

        for (var p = 0; p < publishers.Length; p++)
        {
            publishers[p] = new Thread(() =>
            {
                while (Volatile.Read(ref stopPublishing) == 0)
                {
                    try
                    {
                        EventBus<BusEvent<TagConcurrentChurn>>.Publish(new BusEvent<TagConcurrentChurn>(0));
                    }
                    catch (Exception ex)
                    {
                        publishFailures.Enqueue(ex);

                        return;
                    }
                }
            }) { IsBackground = true };

            publishers[p].Start();
        }

        RunInParallel(churnThreads, _ =>
        {
            for (var i = 0; i < roundsPerThread; i++)
            {
                // Capturing keeps each transient handler a distinct delegate instance, so a
                // botched Unsubscribe leaves a real extra entry behind instead of coincidentally
                // removing an identical one.
                var id = i;

                var transient = new Action<BusEvent<TagConcurrentChurn>>(_ =>
                {
                    if (id < 0) throw new InvalidOperationException();
                });

                EventBus<BusEvent<TagConcurrentChurn>>.Subscribe(transient);
                EventBus<BusEvent<TagConcurrentChurn>>.Unsubscribe(transient);
            }
        });

        Volatile.Write(ref stopPublishing, 1);

        foreach (var publisher in publishers)
            Assert.True(publisher.Join(TimeSpan.FromSeconds(10)), "a publisher thread did not stop");

        Assert.Empty(publishFailures);
        Assert.True(resident.Value > 0, "publishers should have delivered at least one event");

        // Every transient handler was paired with an unsubscribe, so only the resident one is left.
        var before = resident.Value;
        EventBus<BusEvent<TagConcurrentChurn>>.Publish(new BusEvent<TagConcurrentChurn>(0));

        Assert.Equal(before + 1, resident.Value);
    }

    [Fact]
    public void Scope_Dispose_RemovesEveryHandlerItRegistered()
    {
        // Guards the test-suite's own cleanup contract: a leaked handler on a static bus would
        // silently corrupt whichever test ran next on the same event type.
        var counter = new Counter();
        var handler = new Action<BusEvent<TagScopeCleanup>>(_ => counter.Increment());

        using (var scope = new EventBusScope<BusEvent<TagScopeCleanup>>())
        {
            scope.Subscribe(handler);
            scope.Subscribe(handler);
            scope.SubscribeCounter(counter);

            EventBus<BusEvent<TagScopeCleanup>>.Publish(new BusEvent<TagScopeCleanup>(1));

            Assert.Equal(3, counter.Value);
        }

        EventBus<BusEvent<TagScopeCleanup>>.Publish(new BusEvent<TagScopeCleanup>(2));

        Assert.Equal(3, counter.Value);
    }

    /// <summary>
    /// Runs <paramref name="body"/> on <paramref name="threads"/> dedicated threads released from a
    /// common barrier, so the contention the CAS loop is meant to survive actually happens. Uses
    /// real threads rather than <see cref="Parallel"/> because a barrier over pool work items can
    /// deadlock when the pool runs fewer workers than there are participants.
    /// </summary>
    private static void RunInParallel(int threads, Action<int> body)
    {
        using var ready = new Barrier(threads + 1);

        var failures = new ConcurrentQueue<Exception>();
        var workers = new Thread[threads];

        for (var t = 0; t < threads; t++)
        {
            var index = t;

            workers[t] = new Thread(() =>
            {
                try
                {
                    ready.SignalAndWait();
                    body(index);
                }
                catch (Exception ex)
                {
                    failures.Enqueue(ex);
                }
            }) { IsBackground = true };

            workers[t].Start();
        }

        ready.SignalAndWait();

        foreach (var worker in workers)
            Assert.True(worker.Join(TimeSpan.FromSeconds(30)), "a worker thread did not finish in time");

        Assert.Empty(failures);
    }
}
