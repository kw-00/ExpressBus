using System.Buffers;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

public class RequestHandlerBaseTests
{
	/// <summary>
	/// A fake <see cref="IConnection"/> backed by a byte array.
	/// </summary>
	private sealed class FakeConnection : IConnection
	{
		private readonly byte[] _data;
		private int _position;

		public FakeConnection(byte[] data) => _data = data;

		public Task SendAsync(ReadOnlyMemory<byte> data) => Task.CompletedTask;

		public async Task<int> ReceiveAsync(Memory<byte> buffer)
		{
			var available = _data.Length - _position;
			if (available == 0)
			{
				throw new IOException("Connection closed by remote end.");
			}
			var toCopy = Math.Min(buffer.Length, available);
			_data.AsSpan(_position, toCopy).CopyTo(buffer.Span);
			_position += toCopy;
			return toCopy;
		}

		public Task<int> ReceiveFullAsync(Memory<byte> buffer)
		{
			int totalRead = 0;
			while (totalRead < buffer.Length)
			{
				var bytesRead = ReceiveAsync(buffer.Slice(totalRead)).GetAwaiter().GetResult();
				totalRead += bytesRead;
			}
			return Task.FromResult(totalRead);
		}

		public Task CloseAsync(CloseMode mode) => Task.CompletedTask;
		public Action<CloseMode>? Closed { get; set; }
	}

	/// <summary>
	/// Concrete implementation for testing that records which handler was called.
	/// </summary>
	private class TestRequestHandler : RequestHandlerBase
	{
		public string? LastHandled { get; private set; }

		public TestRequestHandler(IConnection connection) : base(connection) { }

		protected override DisposableMemory CreateBuffer(int size) =>
			new DisposableMemory(size);

		protected override BroadcastResponse HandleBroadcastRequest(BroadcastRequest request)
		{
			LastHandled = "Broadcast";
			return new BroadcastResponse(Status.Success, request.RequestId);
		}

		protected override SubscribeResponse HandleSubscribeRequest(SubscribeRequest request)
		{
			LastHandled = "Subscribe";
			return new SubscribeResponse(Status.Success, request.RequestId);
		}

		protected override UnsubscribeResponse HandleUnsubscribeRequest(UnsubscribeRequest request)
		{
			LastHandled = "Unsubscribe";
			return new UnsubscribeResponse(Status.Success, request.RequestId);
		}

		protected override UnsubscribeAllResponse HandleUnsubscribeAllRequest(UnsubscribeAllRequest request)
		{
			LastHandled = "UnsubscribeAll";
			return new UnsubscribeAllResponse(Status.Success, request.RequestId);
		}
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
	public async Task HandleRequestAsync_BroadcastDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(4, new byte[] { 97, 98, 99, 100 });
		var message = new SerializableByteMemory(3, new byte[] { 10, 20, 30 });
		var request = new BroadcastRequest(requestId, topic, message);
		var buffer = new byte[request.ByteSize];
		request.ToBytes(buffer);
		var connection = new FakeConnection(BuildRequestBytes(BroadcastRequest.MessageTypeIdentifier, buffer));
		var handler = new TestRequestHandler(connection);

		// Act
		await handler.HandleRequestAsync();

		// Assert
		Assert.Equal("Broadcast", handler.LastHandled);
	}

	[Fact]
	public async Task HandleRequestAsync_SubscribeDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(5, new byte[] { 1, 2, 3, 4, 5 });
		var request = new SubscribeRequest(requestId, topic);
		var buffer = new byte[request.ByteSize];
		request.ToBytes(buffer);
		var connection = new FakeConnection(BuildRequestBytes(SubscribeRequest.MessageTypeIdentifier, buffer));
		var handler = new TestRequestHandler(connection);

		// Act
		await handler.HandleRequestAsync();

		// Assert
		Assert.Equal("Subscribe", handler.LastHandled);
	}

	[Fact]
	public async Task HandleRequestAsync_UnsubscribeDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(4, new byte[] { 97, 98, 99, 100 });
		var request = new UnsubscribeRequest(requestId, topic);
		var buffer = new byte[request.ByteSize];
		request.ToBytes(buffer);
		var connection = new FakeConnection(BuildRequestBytes(UnsubscribeRequest.MessageTypeIdentifier, buffer));
		var handler = new TestRequestHandler(connection);

		// Act
		await handler.HandleRequestAsync();

		// Assert
		Assert.Equal("Unsubscribe", handler.LastHandled);
	}

	[Fact]
	public async Task HandleRequestAsync_UnsubscribeAllDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var request = new UnsubscribeAllRequest(requestId);
		var buffer = new byte[request.ByteSize];
		request.ToBytes(buffer);
		var connection = new FakeConnection(BuildRequestBytes(UnsubscribeAllRequest.MessageTypeIdentifier, buffer));
		var handler = new TestRequestHandler(connection);

		// Act
		await handler.HandleRequestAsync();

		// Assert
		Assert.Equal("UnsubscribeAll", handler.LastHandled);
	}

	[Fact]
	public async Task HandleRequestAsync_UnknownTypeByte_ThrowsFormatException()
	{
		// Arrange
		var connection = new FakeConnection(new byte[] { 0x63, 0x00, 0x00, 0x00, 0x00 });
		var handler = new TestRequestHandler(connection);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<FormatException>(handler.HandleRequestAsync);
		Assert.Contains("0x63", ex.Message);
	}

	[Fact]
	public async Task HandleRequestAsync_TruncatedMessageSize_ThrowsInvalidDataException()
	{
		// Arrange
		var connection = new FakeConnection(new byte[] { 0x00, 0x01 });
		var handler = new TestRequestHandler(connection);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<IOException>(handler.HandleRequestAsync);
		Assert.Contains("Connection closed", ex.Message);
	}

	[Fact]
	public async Task HandleRequestAsync_TruncatedPayload_ThrowsInvalidDataException()
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
		var handler = new TestRequestHandler(connection);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<IOException>(handler.HandleRequestAsync);
		Assert.Contains("Connection closed", ex.Message);
	}
}
