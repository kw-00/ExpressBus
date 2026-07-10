using System.Buffers;
using System.Collections.Generic;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Provider;
using ExpressBus.Transfer;

public class ConnectionHandlingTests
{
    /// <summary>
    /// A fake <see cref="IConnection"/> backed by a byte array.
    /// </summary>
    private sealed class FakeConnection : IConnection
    {
        private readonly byte[] _data;
        private int _position;
        public List<ReadOnlyMemory<byte>> SentData { get; } = new();

        public FakeConnection(byte[] data) => _data = data;

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            SentData.Add(data.ToArray());
            return Task.CompletedTask;
        }

        public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var available = _data.Length - _position;
            if (available == 0) return Task.FromResult(0);
            
            var toCopy = Math.Min(buffer.Length, available);
            _data.AsSpan(_position, toCopy).CopyTo(buffer.Span);
            _position += toCopy;
            return Task.FromResult(toCopy);
        }

        public Task<int> ReceiveFullAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var bytesRead = ReceiveAsync(buffer.Slice(totalRead), cancellationToken).GetAwaiter().GetResult();
                if (bytesRead == 0) break;
                totalRead += bytesRead;
            }
            return Task.FromResult(totalRead);
        }

        public Task CloseAsync(CloseMode mode) => Task.CompletedTask;
        public Action<CloseMode>? Closed { get; set; }
    }

    private static byte[] BuildRequestBytes(byte typeByte, byte[] payload)
    {
        var sizeBytes = BitConverter.GetBytes(payload.Length);
        var data = new byte[1 + sizeBytes.Length + payload.Length];
        data[0] = typeByte;
        sizeBytes.CopyTo(data, 1);
        payload.CopyTo(data, 1 + sizeBytes.Length);
        return data;
    }

    [Fact]
    public async Task HandleConnectionRequestsAsync_BroadcastDispatched_ResponseSent()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var topic = new SerializableByteMemory(4, new byte[] { 97, 98, 99, 100 });
        var message = new SerializableByteMemory(3, new byte[] { 10, 20, 30 });
        var request = new BroadcastRequest(requestId, topic, message);
        var buffer = new byte[request.ByteSize];
        request.ToBytes(buffer);
        var connection = new FakeConnection(BuildRequestBytes(BroadcastRequest.MessageTypeIdentifier, buffer));
        var handler = new ConnectionHandling(connection, new TopicTracker(), null);

        // Act
        await handler.HandleConnectionRequestsAsync(CancellationToken.None);

        // Assert
        Assert.Single(connection.SentData);
        Assert.True(connection.SentData[0].Length > 0);
    }

    [Fact]
    public async Task HandleConnectionRequestsAsync_SubscribeDispatched_ResponseSent()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var topic = new SerializableByteMemory(5, new byte[] { 1, 2, 3, 4, 5 });
        var request = new SubscribeRequest(requestId, topic);
        var buffer = new byte[request.ByteSize];
        request.ToBytes(buffer);
        var connection = new FakeConnection(BuildRequestBytes(SubscribeRequest.MessageTypeIdentifier, buffer));
        var handler = new ConnectionHandling(connection, new TopicTracker(), null);

        // Act
        await handler.HandleConnectionRequestsAsync(CancellationToken.None);

        // Assert
        Assert.Single(connection.SentData);
    }

    [Fact]
    public async Task HandleConnectionRequestsAsync_UnsubscribeDispatched_ResponseSent()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var topic = new SerializableByteMemory(4, new byte[] { 97, 98, 99, 100 });
        var request = new UnsubscribeRequest(requestId, topic);
        var buffer = new byte[request.ByteSize];
        request.ToBytes(buffer);
        var connection = new FakeConnection(BuildRequestBytes(UnsubscribeRequest.MessageTypeIdentifier, buffer));
        var handler = new ConnectionHandling(connection, new TopicTracker(), null);

        // Act
        await handler.HandleConnectionRequestsAsync(CancellationToken.None);

        // Assert
        Assert.Single(connection.SentData);
    }

    [Fact]
    public async Task HandleConnectionRequestsAsync_UnsubscribeAllDispatched_ResponseSent()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var request = new UnsubscribeAllRequest(requestId);
        var buffer = new byte[request.ByteSize];
        request.ToBytes(buffer);
        var connection = new FakeConnection(BuildRequestBytes(UnsubscribeAllRequest.MessageTypeIdentifier, buffer));
        var handler = new ConnectionHandling(connection, new TopicTracker(), null);

        // Act
        await handler.HandleConnectionRequestsAsync(CancellationToken.None);

        // Assert
        Assert.Single(connection.SentData);
    }

    [Fact]
    public async Task HandleConnectionRequestsAsync_UnknownTypeByte_ThrowsFormatException()
    {
        // Arrange
        var connection = new FakeConnection(new byte[] { 0x63, 0x00, 0x00, 0x00, 0x00 });
        var handler = new ConnectionHandling(connection, new TopicTracker(), null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FormatException>(() => handler.HandleConnectionRequestsAsync(CancellationToken.None));
        Assert.Contains("0x63", ex.Message);
    }

    [Fact]
    public async Task HandleConnectionRequestsAsync_TruncatedMessageSize_HandlesGracefully()
    {
        // Arrange
        var connection = new FakeConnection(new byte[] { 0x00, 0x01 });
        var handler = new ConnectionHandling(connection, new TopicTracker(), null);

        // Act & Assert
        await handler.HandleConnectionRequestsAsync(CancellationToken.None);
    }

    [Fact]
    public async Task HandleConnectionRequestsAsync_TruncatedPayload_HandlesGracefully()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var request = new SubscribeRequest(requestId, new SerializableByteMemory(3, new byte[] { 1, 2, 3 }));
        var buffer = new byte[request.ByteSize];
        request.ToBytes(buffer);

        var halfLen = buffer.Length / 2;
        var halfBuffer = new byte[halfLen];
        Array.Copy(buffer, halfBuffer, halfLen);
        var sizeBytes = BitConverter.GetBytes(buffer.Length);
        var data = new byte[1 + sizeBytes.Length + halfLen];
        data[0] = SubscribeRequest.MessageTypeIdentifier;
        sizeBytes.CopyTo(data, 1);
        halfBuffer.CopyTo(data, 1 + sizeBytes.Length);
        var connection = new FakeConnection(data);
        var handler = new ConnectionHandling(connection, new TopicTracker(), null);

        // Act & Assert
        await handler.HandleConnectionRequestsAsync(CancellationToken.None);
    }
}
