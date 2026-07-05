using System.Buffers;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;

public class NotificationHandlerBaseTests
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
	private class TestNotificationHandler : NotificationHandlerBase
	{
		public BroadcastNotification LastNotification { get; private set; }

		protected override IMemoryOwner<byte> CreateBuffer(int size) =>
			new ExactMemoryOwner(new byte[size]);

		protected override Task HandleBroadcastNotificationAsync(BroadcastNotification notification)
		{
			LastNotification = notification;
			return Task.CompletedTask;
		}
	}

	private static MemoryStream BuildNotificationStream(byte typeByte, byte[] payload)
	{
		var sizeBytes = BitConverter.GetBytes(payload.Length);
		var data = new byte[1 + sizeBytes.Length + payload.Length];
		data[0] = typeByte;
		sizeBytes.CopyTo(data, 1);
		payload.CopyTo(data, 1 + sizeBytes.Length);
		return new MemoryStream(data);
	}

	[Fact]
	public async Task HandleNotificationAsync_BroadcastDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var handler = new TestNotificationHandler();
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(6, new byte[] { 1, 2, 3, 4, 5, 6 });
		var message = new SerializableByteMemory(2, new byte[] { 7, 8 });
		var notification = new BroadcastNotification(requestId, topic, message);
		var buffer = new byte[notification.ByteSize];
		notification.ToBytes(buffer);
		var stream = BuildNotificationStream(BroadcastNotification.MessageTypeIdentifier, buffer);

		// Act
		await handler.HandleNotificationAsync(stream);

		// Assert
		Assert.Equal(notification.RequestId, handler.LastNotification.RequestId);
	}

	[Fact]
	public async Task HandleNotificationAsync_UnknownTypeByte_ThrowsFormatException()
	{
		// Arrange
		var handler = new TestNotificationHandler();
		var stream = new MemoryStream(new byte[] { 0x63, 0x00, 0x00, 0x00, 0x00 });
		stream.Position = 0;

		// Act & Assert
		var ex = await Assert.ThrowsAsync<FormatException>(() => handler.HandleNotificationAsync(stream));
		Assert.Contains("0x63", ex.Message);
	}

	[Fact]
	public async Task HandleNotificationAsync_TruncatedMessageSize_ThrowsInvalidDataException()
	{
		// Arrange
		var handler = new TestNotificationHandler();
		var stream = new MemoryStream(new byte[] { 0x00, 0x01 });
		stream.Position = 0;

		// Act & Assert
		var ex = await Assert.ThrowsAsync<InvalidDataException>(() => handler.HandleNotificationAsync(stream));
		Assert.Contains("Unexpected end of stream", ex.Message);
	}

	[Fact]
	public async Task HandleNotificationAsync_TruncatedPayload_ThrowsInvalidDataException()
	{
		// Arrange
		var handler = new TestNotificationHandler();
		var requestId = Guid.NewGuid();
		var notification = new BroadcastNotification(
			requestId,
			new SerializableByteMemory(3, new byte[] { 1, 2, 3 }),
			new SerializableByteMemory(2, new byte[] { 4, 5 }));
		var buffer = new byte[notification.ByteSize];
		notification.ToBytes(buffer);

		var halfLen = buffer.Length / 2;
		var halfBuffer = new byte[halfLen];
		Array.Copy(buffer, halfBuffer, halfLen);
		var sizeBytes = BitConverter.GetBytes(buffer.Length);
		var data = new byte[1 + sizeBytes.Length + halfLen];
		data[0] = BroadcastNotification.MessageTypeIdentifier;
		sizeBytes.CopyTo(data, 1);
		halfBuffer.CopyTo(data, 1 + sizeBytes.Length);
		var stream = new MemoryStream(data);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<InvalidDataException>(() => handler.HandleNotificationAsync(stream));
		Assert.Contains("Unexpected end of stream", ex.Message);
	}
}
