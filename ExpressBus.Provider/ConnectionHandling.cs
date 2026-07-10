using System.Buffers.Binary;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

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

    public async Task HandleConnectionRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Read 1-byte type identifier to dispatch
            var typeBuffer = CreateBuffer(1);
            if (await _connection.ReceiveFullAsync(typeBuffer.Memory, cancellationToken).ConfigureAwait(false) < typeBuffer.Memory.Length) break;
            var typeByte = typeBuffer.Memory.Span[0];
            typeBuffer.Dispose();

            // Read 4-byte message size (LE int32)
            var sizeBuffer = CreateBuffer(4);
            if (await _connection.ReceiveFullAsync(sizeBuffer.Memory, cancellationToken).ConfigureAwait(false) < sizeBuffer.Memory.Length) break;
            var messageSize = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer.Memory.Span);
            sizeBuffer.Dispose();

            // Read remaining payload (messageSize - 5 bytes, since frame is 5 bytes)
            var remainingSize = messageSize - 5;
            var fullBuffer = CreateBuffer(messageSize);
            fullBuffer.Memory.Span[0] = typeByte;
            BinaryPrimitives.WriteInt32LittleEndian(fullBuffer.Memory.Span.Slice(1), messageSize);
            if (await _connection.ReceiveFullAsync(fullBuffer.Memory.Slice(5), cancellationToken).ConfigureAwait(false) < remainingSize) break;
            var requestBytes = fullBuffer.Memory;

            DisposableMemory response;
            if (typeByte == BroadcastRequest.MessageTypeIdentifier)
                response = SerializeResponse(HandleBroadcastRequest(BroadcastRequest.FromBytes(requestBytes)));
            else if (typeByte == SubscribeRequest.MessageTypeIdentifier)
                response = SerializeResponse(HandleSubscribeRequest(SubscribeRequest.FromBytes(requestBytes)));
            else if (typeByte == UnsubscribeRequest.MessageTypeIdentifier)
                response = SerializeResponse(HandleUnsubscribeRequest(UnsubscribeRequest.FromBytes(requestBytes)));
            else if (typeByte == UnsubscribeAllRequest.MessageTypeIdentifier)
                response = SerializeResponse(HandleUnsubscribeAllRequest(UnsubscribeAllRequest.FromBytes(requestBytes)));
            else
                throw new FormatException($"Unknown MessageTypeIdentifier: 0x{typeByte:X2}");

            await _connection.SendAsync(response.Memory, cancellationToken).ConfigureAwait(false);
            response.Dispose();
        }
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

    private BroadcastResponse HandleBroadcastRequest(BroadcastRequest request)
    {
        var notification = new EventNotification(request.Topic, request.Message);
        var notificationSize = notification.ByteSize;
        var wireOwner = CreateBuffer(notificationSize);
        notification.ToBytes(wireOwner.Memory);
        var wireMem = wireOwner.Memory.Slice(0, notificationSize);

        var subscribers = _topicTracker.GetSubscribers(request.Topic.Memory);
        subscribers.Remove(_connection);

        foreach (var subscriber in subscribers)
        {
            try
            {
                subscriber.SendAsync(wireMem).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send event notification to subscriber");
            }
        }

        wireOwner.Dispose();

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
