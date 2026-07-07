using System.Buffers;
using System.Collections.Concurrent;
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
    private readonly object _lock = new();
    private Task? _notificationLoopTask;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="EstablishConnection"/> and starts the notification read loop.
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
            new TopicKeyComparer());

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
    /// Subscribes to events on the specified topic.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handler">The action to invoke when an event notification is received for this topic.</param>
    public void Subscribe(ReadOnlyMemory<byte> topic, Action<ReadOnlyMemory<byte>> handler)
    {
        lock (_lock)
        {
            _eventHandlers.Set(topic, handler);

            var isNewTopic = _topicHandlerCount.AddOrUpdate(topic,
                _ => 1,
                (_, count) => count + 1) == 1;

            if (isNewTopic)
            {
                var request = new SubscribeRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic));
                _requestSender.SendSubscribeRequestAsync(request).GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>
    /// Unsubscribes from the specified topic.
    /// </summary>
    /// <param name="topic">The topic to unsubscribe from.</param>
    public void Unsubscribe(ReadOnlyMemory<byte> topic)
    {
        lock (_lock)
        {
            _eventHandlers.Remove(topic);

            var newCount = _topicHandlerCount.AddOrUpdate(topic,
                _ => 0,
                (_, count) => Math.Max(0, count - 1));

            if (newCount == 0)
            {
                _topicHandlerCount.TryRemove(topic, out _);
                var request = new UnsubscribeRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic));
                _requestSender.SendUnsubscribeRequestAsync(request).GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>
    /// Broadcasts a message to all subscribers of the specified topic.
    /// </summary>
    /// <param name="topic">The topic to broadcast to.</param>
    /// <param name="message">The message payload to broadcast.</param>
    public void Broadcast(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> message)
    {
        lock (_lock)
        {
            var request = new BroadcastRequest(Guid.NewGuid(), new SerializableByteMemory(topic.Length, topic), new SerializableByteMemory(message.Length, message));
            _requestSender.SendBroadcastRequestAsync(request).GetAwaiter().GetResult();
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
    }

    /// <summary>
    /// Custom comparer that hashes and compares <see cref="ReadOnlyMemory{T}"/> by contents.
    /// </summary>
    internal sealed class TopicKeyComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public static readonly TopicKeyComparer Instance = new();
        internal TopicKeyComparer() { }

        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            if (x.Length != y.Length)
                return false;
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode(ReadOnlyMemory<byte> obj)
        {
            var span = obj.Span;
            var hc = new System.HashCode();
            for (var i = 0; i < span.Length; i++)
                hc.Add(span[i]);
            return hc.ToHashCode();
        }
    }
}
