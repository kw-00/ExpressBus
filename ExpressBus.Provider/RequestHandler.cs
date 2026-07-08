using System.Buffers;
using System.Buffers.Binary;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

namespace ExpressBus.Provider;

/// <summary>
/// Concrete server-side request handler for the ExpressBus messaging protocol.
/// </summary>
/// <remarks>
/// Each connection receives its own <see cref="RequestHandler"/> instance (injected via the
/// base class constructor). The handler uses <see cref="MemoryPool{T}.Shared"/> for buffer
/// allocation and delegates subscription management to a shared <see cref="TopicTracker"/>.
/// </remarks>
public sealed class RequestHandler : RequestHandlerBase
{
    private readonly TopicTracker _topicTracker;
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a new <see cref="RequestHandler"/> for the specified connection.
    /// </summary>
    /// <param name="connection">The connection this handler instance is dedicated to.</param>
    /// <param name="topicTracker">The shared topic tracker for managing subscriptions.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public RequestHandler(IConnection connection, TopicTracker topicTracker, ILogger? logger = null)
        : base(connection)
    {
        _topicTracker = topicTracker;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override DisposableMemory CreateBuffer(int size) =>
        new DisposableMemory(size);

    /// <inheritdoc />
    protected override BroadcastResponse HandleBroadcastRequest(BroadcastRequest request)
    {
        // Build the event notification payload
        var notification = new EventNotification(request.Topic, request.Message);
        var notificationSize = notification.ByteSize;

        // Serialize notification into a buffer
        var notifOwner = CreateBuffer(notificationSize);
        notification.ToBytes(notifOwner.Memory);
        var notifBytes = notifOwner.Memory.Slice(0, notificationSize);

        // Build the full wire frame: 1 byte type + 4 bytes size + payload
        var wireSize = 5 + notifBytes.Length;
        var wireOwner = CreateBuffer(wireSize);
        var wireMem = wireOwner.Memory;
        wireMem.Span[0] = EventNotification.MessageTypeIdentifier;
        BinaryPrimitives.WriteInt32LittleEndian(wireMem.Span.Slice(1), notifBytes.Length);
        notifBytes.Span.CopyTo(wireMem.Span.Slice(5));

        // Get subscribers (excluding the sender)
        var subscribers = _topicTracker.GetSubscribers(request.Topic.Data);
        subscribers.Remove(Connection);

        // Send to all subscribers (best-effort: catch per-subscriber exceptions)
        foreach (var subscriber in subscribers)
        {
            try
            {
                subscriber.SendAsync(wireMem.Slice(0, wireSize)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send event notification to subscriber");
            }
        }

        notifOwner.Dispose();
        wireOwner.Dispose();

        return new BroadcastResponse(Status.Success, request.RequestId);
    }

    /// <inheritdoc />
    protected override SubscribeResponse HandleSubscribeRequest(SubscribeRequest request)
    {
        _topicTracker.AddSubscriber(request.Topic.Data, Connection);
        return new SubscribeResponse(Status.Success, request.RequestId);
    }

    /// <inheritdoc />
    protected override UnsubscribeResponse HandleUnsubscribeRequest(UnsubscribeRequest request)
    {
        _topicTracker.RemoveSubscriber(request.Topic.Data, Connection);
        return new UnsubscribeResponse(Status.Success, request.RequestId);
    }

    /// <inheritdoc />
    protected override UnsubscribeAllResponse HandleUnsubscribeAllRequest(UnsubscribeAllRequest request)
    {
        _topicTracker.RemoveSubscriber(Connection);
        return new UnsubscribeAllResponse(Status.Success, request.RequestId);
    }
}
