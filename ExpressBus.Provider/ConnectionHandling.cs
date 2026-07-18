using System.Buffers.Binary;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;
using System.Threading.Tasks;

namespace ExpressBus.Provider;

/// <summary>
/// Handles request/response processing for a single client connection.
/// </summary>
public sealed class ConnectionHandling
{
    private readonly IConnection _connection;
    private readonly TopicTracker _topicTracker;
    private readonly ILogger? _logger;

    public ConnectionHandling(IConnection connection, TopicTracker topicTracker, ILogger? logger)
    {
        _connection = connection;
        _topicTracker = topicTracker;
        _logger = logger;
    }

    public async Task HandleRequestAsync(CancellationToken cancellationToken)
    {
        // Read 5-byte header: 1-byte type + 4-byte message size (LE int32)
        using var headerBuffer = CreateBuffer(5);
        await _connection.ReceiveFullAsync(headerBuffer.Memory, cancellationToken).ConfigureAwait(false);
        var typeByte = headerBuffer.Memory.Span[0];
        var messageSize = BinaryPrimitives.ReadInt32LittleEndian(headerBuffer.Memory.Span.Slice(1));

        // Read full message (header + payload)
        using var fullBuffer = CreateBuffer(messageSize);
        headerBuffer.Memory.Span.CopyTo(fullBuffer.Memory.Span);
        await _connection.ReceiveFullAsync(fullBuffer.Memory.Slice(5), cancellationToken).ConfigureAwait(false);

        using var response =
            typeByte == BroadcastRequest.MessageTypeIdentifier ? SerializeResponse(await HandleBroadcastRequestAsync(BroadcastRequest.FromBytes(fullBuffer.Memory)).ConfigureAwait(false)) :
            typeByte == SubscribeRequest.MessageTypeIdentifier ? SerializeResponse(HandleSubscribeRequest(SubscribeRequest.FromBytes(fullBuffer.Memory))) :
            typeByte == UnsubscribeRequest.MessageTypeIdentifier ? SerializeResponse(HandleUnsubscribeRequest(UnsubscribeRequest.FromBytes(fullBuffer.Memory))) :
            typeByte == UnsubscribeAllRequest.MessageTypeIdentifier ? SerializeResponse(HandleUnsubscribeAllRequest(UnsubscribeAllRequest.FromBytes(fullBuffer.Memory))) :
            throw new FormatException($"Unknown MessageTypeIdentifier: 0x{typeByte:X2}");

        await _connection.SendAsync(response.Memory, cancellationToken).ConfigureAwait(false);
    }

    private DisposableMemory CreateBuffer(int size) => new DisposableMemory(size);

    private DisposableMemory SerializeResponse<T>(T response)
        where T : struct, IByteSerializable<T>, IMessageSize
    {
        var exactSize = response.ByteSize;
        var owner = CreateBuffer(exactSize);
        response.ToBytes(owner.Memory);
        return owner;
    }

    private async Task<BroadcastResponse> HandleBroadcastRequestAsync(BroadcastRequest request)
    {
        var notification = new EventNotification(request.Topic, request.Message);
        using var wireOwner = CreateBuffer(notification.ByteSize);
        notification.ToBytes(wireOwner.Memory);

        var subscribers = _topicTracker.GetSubscribers(request.Topic.Memory);
        subscribers.Remove(_connection);

        foreach (var subscriber in subscribers)
        {
            try
            {
                await subscriber.SendAsync(wireOwner.Memory).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send event notification to subscriber");
            }
        }

        return new BroadcastResponse(Status.Success, request.RequestId);
    }

    private SubscribeResponse HandleSubscribeRequest(SubscribeRequest request)
    {
        _topicTracker.AddSubscriber(request.Topic.Memory, _connection);
        return new SubscribeResponse(Status.Success, request.RequestId);
    }

    private UnsubscribeResponse HandleUnsubscribeRequest(UnsubscribeRequest request)
    {
        _topicTracker.RemoveSubscriber(request.Topic.Memory, _connection);
        return new UnsubscribeResponse(Status.Success, request.RequestId);
    }

    private UnsubscribeAllResponse HandleUnsubscribeAllRequest(UnsubscribeAllRequest request)
    {
        _topicTracker.RemoveSubscriber(_connection);
        return new UnsubscribeAllResponse(Status.Success, request.RequestId);
    }
}
