using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using ExpressBus.DataStructures;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

namespace ExpressBus.Client;

/// <summary>
/// Abstract base class for ExpressBus message broker clients.
/// </summary>
/// <remarks>
/// Each instance establishes a connection to a broker at the specified <see cref="Address"/>
/// and provides high-level pub/sub operations. Subclasses implement <see cref="EstablishConnection"/>
/// to provide the transport mechanism (e.g., TCP).
/// </remarks>
public abstract class ExpressBusClientBase : IAsyncDisposable
{
    private readonly Address _address;
    private readonly IConnection _connection;
    private readonly RequestSender _requestSender;
    private readonly ClientNotificationHandler _notificationHandler;
    private readonly EventHandlers _eventHandlers;
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, int> _topicHandlerCount;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Task? _notificationLoopTask;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="ExpressBusClientBase"/> and starts the notification read loop.
    /// </summary>
    /// <param name="address">The broker address to connect to.</param>
    protected ExpressBusClientBase(Address address)
    {
        _address = address;
        _connection = EstablishConnection();
        _requestSender = new RequestSender(_connection);
        _eventHandlers = new EventHandlers();
        _notificationHandler = new ClientNotificationHandler(_connection, _eventHandlers);
        _topicHandlerCount = new ConcurrentDictionary<ReadOnlyMemory<byte>, int>(
            MemoryComparer<byte>.Instance);

        _notificationLoopTask = Task.Run(NotificationLoopAsync);
    }

    /// <summary>
    /// Establishes a connection to the message broker.
    /// </summary>
    /// <remarks>
    /// Implement this method in a derived class to create and return an <see cref="IConnection"/>
    /// configured for the desired transport (e.g., TCP).
    /// </remarks>
    /// <returns>A connected <see cref="IConnection"/> to the broker.</returns>
    protected abstract IConnection EstablishConnection();

    /// <summary>
    /// Subscribes to events on the specified topic. Multiple handlers can be registered
    /// for the same topic. A broker-side subscription is sent only when the first handler
    /// is registered for a given topic.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handler">The action to invoke when an event notification is received for this topic.</param>
    public async Task SubscribeAsync(ReadOnlyMemory<byte> topic, Action<ReadOnlyMemory<byte>> handler)
    {
        await _lock.WaitAsync();
        try
        {
            _eventHandlers.Set(topic, handler);

            var isNewTopic = _topicHandlerCount.AddOrUpdate(topic,
                _ => 1,
                (_, count) => count + 1) == 1;

            if (isNewTopic)
            {
                var request = new SubscribeRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic));
                await _requestSender.SendSubscribeRequestAsync(request).ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Unsubscribes from the specified topic, removing all registered handlers.
    /// A broker-side unsubscribe is sent only when the last handler is removed.
    /// </summary>
    /// <param name="topic">The topic to unsubscribe from.</param>
    public async Task UnsubscribeAsync(ReadOnlyMemory<byte> topic)
    {
        await _lock.WaitAsync();
        try
        {
            var handlers = _eventHandlers.GetHandlers(topic);
            foreach (var handler in handlers)
            {
                _eventHandlers.Remove(topic, handler);
            }

            var newCount = _topicHandlerCount.AddOrUpdate(topic,
                _ => 0,
                (_, count) => Math.Max(0, count - 1));

            if (newCount == 0)
            {
                _topicHandlerCount.TryRemove(topic, out _);
                var request = new UnsubscribeRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic));
                await _requestSender.SendUnsubscribeRequestAsync(request).ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Broadcasts a message to all subscribers of the specified topic.
    /// </summary>
    /// <param name="topic">The topic to broadcast to.</param>
    /// <param name="message">The message payload to broadcast.</param>
    public async Task BroadcastAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> message)
    {
        await _lock.WaitAsync();
        try
        {
            var request = new BroadcastRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic), new SerializableByteMemory(message.Length, message));
            await _requestSender.SendBroadcastRequestAsync(request).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task NotificationLoopAsync()
    {
        while (!_disposed)
        {
            try
            {
                await _notificationHandler.HandleNotificationAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Connection closed or error — exit the loop
                break;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Best-effort: unsubscribe from all tracked topics
            foreach (var topic in _topicHandlerCount.Keys)
            {
                try
                {
                    var request = new UnsubscribeAllRequest(Guid.NewGuid());
                    await _requestSender.SendUnsubscribeAllRequestAsync(request).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            await _connection.CloseAsync(CloseMode.Shutdown).ConfigureAwait(false);
        }
        catch
        {
            // Ignore disposal errors
        }
        finally
        {
            await ((IAsyncDisposable)_lock).DisposeAsync().ConfigureAwait(false);
        }
    }
}
