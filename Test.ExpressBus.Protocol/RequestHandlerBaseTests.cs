using System.Buffers;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;

public class RequestHandlerBaseTests
{
	/// <summary>
	/// Wraps a byte array so that its <see cref="Memory.Length"/> exactly matches
	/// the array length — unlike <see cref="MemoryPool{T}.Rent"/> which returns
	/// a buffer that may be larger than requested.
	/// </summary>
	private sealed class ExactMemoryOwner : IMemoryOwner<byte>
	{
		private readonly byte[] _buffer;
		public ExactMemoryOwner(byte[] buffer) => _buffer = buffer;
		public Memory<byte> Memory => _buffer.AsMemory();
		public void Dispose() { }
	}

	/// <summary>
	/// Concrete implementation for testing that records which handler was called.
	/// </summary>
	private class TestRequestHandler : RequestHandlerBase
	{
		public string? LastHandled { get; private set; }

		protected override IMemoryOwner<byte> CreateBuffer(int size) =>
			new ExactMemoryOwner(new byte[size]);

		protected override BroadcastResponse HandleBroadcastRequest(BroadcastRequest request)
		{
			LastHandled = "Broadcast";
			return new BroadcastResponse(request.RequestId);
		}

		protected override SubscribeResponse HandleSubscribeRequest(SubscribeRequest request)
		{
			LastHandled = "Subscribe";
			return new SubscribeResponse(request.RequestId);
		}

		protected override UnsubscribeResponse HandleUnsubscribeRequest(UnsubscribeRequest request)
		{
			LastHandled = "Unsubscribe";
			return new UnsubscribeResponse(request.Status, request.RequestId);
		}

		protected override UnsubscribeAllResponse HandleUnsubscribeAllRequest(UnsubscribeAllRequest request)
		{
			LastHandled = "UnsubscribeAll";
			return new UnsubscribeAllResponse(request.Status, request.RequestId);
		}
	}

	private static MemoryStream BuildRequestStream(byte typeByte, byte[] payload)
	{
		var sizeBytes = BitConverter.GetBytes(payload.Length);
		var data = new byte[1 + sizeBytes.Length + payload.Length];
		data[0] = typeByte;
		sizeBytes.CopyTo(data, 1);
		payload.CopyTo(data, 1 + sizeBytes.Length);
		return new MemoryStream(data);
	}

	[Fact]
	public async Task HandleRequestAsync_BroadcastDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var handler = new TestRequestHandler();
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(4, new byte[] { 97, 98, 99, 100 });
		var message = new SerializableByteMemory(3, new byte[] { 10, 20, 30 });
		var request = new BroadcastRequest(requestId, topic, message);
		var buffer = new byte[request.ByteSize];
		request.ToBytes(buffer);
		var stream = BuildRequestStream(BroadcastRequest.MessageTypeIdentifier, buffer);

		// Act
		await handler.HandleRequestAsync(stream);

		// Assert
		Assert.Equal("Broadcast", handler.LastHandled);
	}

	[Fact]
	public async Task HandleRequestAsync_SubscribeDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var handler = new TestRequestHandler();
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(5, new byte[] { 1, 2, 3, 4, 5 });
		var request = new SubscribeRequest(requestId, topic);
		var buffer = new byte[request.ByteSize];
		request.ToBytes(buffer);
		var stream = BuildRequestStream(SubscribeRequest.MessageTypeIdentifier, buffer);

		// Act
		await handler.HandleRequestAsync(stream);

		// Assert
		Assert.Equal("Subscribe", handler.LastHandled);
	}

	[Fact]
	public async Task HandleRequestAsync_UnsubscribeDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var handler = new TestRequestHandler();
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(4, new byte[] { 97, 98, 99, 100 });
		var request = new UnsubscribeRequest(Status.Success, requestId, topic);
		var buffer = new byte[request.ByteSize];
		request.ToBytes(buffer);
		var stream = BuildRequestStream(UnsubscribeRequest.MessageTypeIdentifier, buffer);

		// Act
		await handler.HandleRequestAsync(stream);

		// Assert
		Assert.Equal("Unsubscribe", handler.LastHandled);
	}

	[Fact]
	public async Task HandleRequestAsync_UnsubscribeAllDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var handler = new TestRequestHandler();
		var requestId = Guid.NewGuid();
		var request = new UnsubscribeAllRequest(Status.Error, requestId);
		var buffer = new byte[request.ByteSize];
		request.ToBytes(buffer);
		var stream = BuildRequestStream(UnsubscribeAllRequest.MessageTypeIdentifier, buffer);

		// Act
		await handler.HandleRequestAsync(stream);

		// Assert
		Assert.Equal("UnsubscribeAll", handler.LastHandled);
	}

	[Fact]
	public async Task HandleRequestAsync_UnknownTypeByte_ThrowsFormatException()
	{
		// Arrange
		var handler = new TestRequestHandler();
		var stream = new MemoryStream(new byte[] { 0x63, 0x00, 0x00, 0x00, 0x00 });
		stream.Position = 0;

		// Act & Assert
		var ex = await Assert.ThrowsAsync<FormatException>(() => handler.HandleRequestAsync(stream));
		Assert.Contains("0x63", ex.Message);
	}

	[Fact]
	public async Task HandleRequestAsync_TruncatedMessageSize_ThrowsInvalidDataException()
	{
		// Arrange
		var handler = new TestRequestHandler();
		var stream = new MemoryStream(new byte[] { 0x00, 0x01 });
		stream.Position = 0;

		// Act & Assert
		var ex = await Assert.ThrowsAsync<InvalidDataException>(() => handler.HandleRequestAsync(stream));
		Assert.Contains("Unexpected end of stream", ex.Message);
	}

	[Fact]
	public async Task HandleRequestAsync_TruncatedPayload_ThrowsInvalidDataException()
	{
		// Arrange
		var handler = new TestRequestHandler();
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
		var stream = new MemoryStream(data);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<InvalidDataException>(() => handler.HandleRequestAsync(stream));
		Assert.Contains("Unexpected end of stream", ex.Message);
	}

}
