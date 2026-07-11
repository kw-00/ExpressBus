using System.Threading;
using ExpressBus.Concurrency;
using ExpressBus.DataStructures;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

namespace ExpressBus.Client;

/// <summary>
/// High-level ExpressBus client that bridges <see cref="ClientMessenger"/> (low-level I/O)
/// with <see cref="EventHandlers"/> (local handler registry).
/// </summary>
/// <remarks>
/// Manages subscriptions on the broker automatically: subscribes when the first handler is
/// registered for a topic, and unsubscribes when the last handler is removed.
///
/// Thread-safety is enforced via a two-level locking scheme:
/// <list type="bullet">
///   <item><description>A <see cref="ReaderWriterLockSlim"/> bulk lock serializes operations
///     that span all topics (e.g. <see cref="UnsubscribeAllAsync"/>) with per-topic operations.</description></item>
///   <item><description>A <see cref="PartitionedReaderWriterLock{T}"/> provides per-topic
///     locking for subscribe/unsubscribe (write) vs. broadcast/invocation (read).</description></item>
/// </list>
/// Lock ordering is always: bulk lock first, then partitioned lock.
/// </remarks>
public sealed class ExpressBusClient : IAsyncDisposable
{
    private readonly IConnectionFactory _factory;
    private readonly Address _address;
    private readonly EventHandlers _handlers;
    private readonly PartitionedReaderWriterLock<ReadOnlyMemory<byte>> _partitionedLock;
    private readonly ReaderWriterLockSlim _bulkLock = new();

    private volatile bool _started;
    private ClientMessenger? _messenger;

    private ClientMessenger Messenger => _messenger ?? throw new InvalidOperationException("Client not started. Call StartAsync().");

    /// <summary>
    /// Creates an <see cref="ExpressBusClient"/> backed by the given factory and address.
    /// </summary>
    /// <param name="factory">The factory used to create the underlying connection.</param>
    /// <param name="address">The remote address to connect to.</param>
    public ExpressBusClient(IConnectionFactory factory, Address address)
        : this(factory, address, new EventHandlers())
    {
    }

    /// <summary>
    /// Creates an <see cref="ExpressBusClient"/> with a custom handler registry.
    /// </summary>
    /// <param name="factory">The factory used to create the underlying connection.</param>
    /// <param name="address">The remote address to connect to.</param>
    /// <param name="handlers">The event handler registry.</param>
    public ExpressBusClient(IConnectionFactory factory, Address address, EventHandlers handlers)
    {
        _factory = factory;
        _address = address;
        _handlers = handlers;
        _partitionedLock = new PartitionedReaderWriterLock<ReadOnlyMemory<byte>>(
            16, HashProducers.ForReadOnlyMemoryByte);
    }

    /// <summary>
    /// Establishes the connection to the broker, starts the event listener,
    /// and subscribes to all topics that have registered handlers.
    /// </summary>
    /// <remarks>
    /// If any broker subscription fails, the messenger is disposed and the
    /// connection is closed — the broker will clean up all subscriptions.
    /// The client remains in a clean (not-started) state and may be retried.
    /// </remarks>
    public async Task StartAsync()
    {
        _bulkLock.EnterWriteLock();
        try
        {
            if (_started)
                return;
            _messenger = new ClientMessenger(_factory, _address);
            _messenger.Event = OnEventNotificationAsync;
            await _messenger.StartAsync().ConfigureAwait(false);

            var topics = _handlers.GetTopics();
            try
            {
                foreach (var topic in topics)
                {
                    await _messenger.SendSubscribeRequestAsync(
                            new SubscribeRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic)))
                        .ConfigureAwait(false);
                }

                _started = true;
            }
            catch
            {
                // Dispose the messenger to disconnect — broker will clean up all subscriptions.
                await _messenger.DisposeAsync().ConfigureAwait(false);
                _messenger = null;

                throw;
            }
        }
        finally
        {
            _bulkLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Stops the client: clears all local handlers and disposes the underlying messenger.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_started)
            return;

        _started = false;

        _bulkLock.EnterWriteLock();
        try
        {
            _handlers.Clear();
        }
        finally
        {
            _bulkLock.ExitWriteLock();
        }

        await Messenger.DisposeAsync().ConfigureAwait(false);
        _messenger = null;
    }

    /// <summary>
    /// Registers an async handler for the specified topic.
    /// </summary>
    /// <remarks>
    /// If this is the first handler for the topic, a subscription is sent to the broker.
    /// If the broker subscription fails, the handler is rolled back (removed locally).
    /// </remarks>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handler">The async action invoked when a notification arrives for this topic.</param>
    public async Task SubscribeAsync(ReadOnlyMemory<byte> topic, Func<ReadOnlyMemory<byte>, Task> handler)
    {
        _bulkLock.EnterReadLock();
        try
        {
            _partitionedLock.AcquireWrite(topic);
            try
            {
                var existing = _handlers.GetHandlers(topic);
                _handlers.Set(topic, handler);

                if (existing.Count == 0)
                {
                    await Messenger.SendSubscribeRequestAsync(
                        new SubscribeRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic)))
                        .ConfigureAwait(false);
                }
            }
            catch
            {
                _handlers.Remove(topic, handler);
                throw;
            }
            finally
            {
                _partitionedLock.ReleaseWrite(topic);
            }
        }
        finally
        {
            _bulkLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Removes all handlers for the specified topic and unsubscribes from the broker.
    /// </summary>
    /// <remarks>
    /// If no handlers are registered for the topic, this is a no-op.
    /// If the broker unsubscription fails, handlers are rolled back (re-added locally).
    /// </remarks>
    /// <param name="topic">The topic to unsubscribe from.</param>
    public async Task UnsubscribeAsync(ReadOnlyMemory<byte> topic)
    {
        _bulkLock.EnterReadLock();
        try
        {
            _partitionedLock.AcquireWrite(topic);
            var existing = new HashSet<Func<ReadOnlyMemory<byte>, Task>>();
            try
            {
                existing = _handlers.GetHandlers(topic);
                if (existing.Count == 0)
                    return;

                _handlers.Remove(topic);

                await Messenger.SendUnsubscribeRequestAsync(
                    new UnsubscribeRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic)))
                    .ConfigureAwait(false);
            }
            catch
            {
                foreach (var h in existing)
                    _handlers.Set(topic, h);
                throw;
            }
            finally
            {
                _partitionedLock.ReleaseWrite(topic);
            }
        }
        finally
        {
            _bulkLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Removes all handlers for all topics and unsubscribes from the broker.
    /// </summary>
    public async Task UnsubscribeAllAsync()
    {
        _bulkLock.EnterWriteLock();
        try
        {
            _handlers.Clear();

            await Messenger.SendUnsubscribeAllRequestAsync(
                new UnsubscribeAllRequest(Guid.NewGuid()))
                .ConfigureAwait(false);
        }
        finally
        {
            _bulkLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Broadcasts a message to all subscribers of the specified topic on the broker.
    /// </summary>
    /// <param name="topic">The topic to broadcast to.</param>
    /// <param name="message">The message payload to broadcast.</param>
    public async Task BroadcastAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> message)
    {
        _bulkLock.EnterReadLock();
        try
        {
            await Messenger.SendBroadcastRequestAsync(
                new BroadcastRequest(
                    Guid.NewGuid(),
                    new SerializableByteMemory(topic.Length, topic),
                    new SerializableByteMemory(message.Length, message)))
                .ConfigureAwait(false);
        }
        finally
        {
            _bulkLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Invoked by the underlying messenger when an event notification arrives from the broker.
    /// Dispatches the notification to all registered handlers for the topic.
    /// </summary>
    private async Task OnEventNotificationAsync(EventNotification notification)
    {
        var handlers = default(HashSet<Func<ReadOnlyMemory<byte>, Task>>);

        _bulkLock.EnterReadLock();
        try
        {
            _partitionedLock.AcquireRead(notification.Topic.Memory);
            try
            {
                handlers = _handlers.GetHandlers(notification.Topic.Memory);
            }
            finally
            {
                _partitionedLock.ReleaseRead(notification.Topic.Memory);
            }
        }
        finally
        {
            _bulkLock.ExitReadLock();
        }

        // Invoke handlers outside all locks to avoid deadlock if a handler
        // calls back into this client (e.g. SubscribeAsync, UnsubscribeAsync).
        var tasks = new Task[handlers.Count];
        var i = 0;
        foreach (var handler in handlers)
        {
            tasks[i++] = handler(notification.Message.Memory);
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _bulkLock.Dispose();
    }
}
