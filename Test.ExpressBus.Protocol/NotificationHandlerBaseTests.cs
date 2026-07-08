using System.Buffers;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

public class NotificationHandlerBaseTests
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
	private class TestNotificationHandler : NotificationHandlerBase
	{
		public EventNotification LastNotification { get; private set; }

		public TestNotificationHandler(IConnection connection)
			: base(connection) { }

		protected override DisposableMemory CreateBuffer(int size) =>
			new DisposableMemory(size);

		protected override Task HandleEventNotificationAsync(EventNotification notification)
		{
			LastNotification = notification;
			return Task.CompletedTask;
		}
	}

	private static byte[] BuildNotificationBytes(byte typeByte, byte[] payload)
	{
		var sizeBytes = BitConverter.GetBytes(payload.Length);
		var data = new byte[1 + sizeBytes.Length + payload.Length];
		data[0] = typeByte;
		sizeBytes.CopyTo(data, 1);
		payload.CopyTo(data, 1 + sizeBytes.Length);
		return data;
	}

	[Fact]
	public async Task HandleNotificationAsync_BroadcastDispatched_CorrectHandlerCalled()
	{
		// Arrange
		var topic = new SerializableByteMemory(6, new byte[] { 1, 2, 3, 4, 5, 6 });
		var message = new SerializableByteMemory(2, new byte[] { 7, 8 });
		var notification = new EventNotification(topic, message);
		var buffer = new byte[notification.ByteSize];
		notification.ToBytes(buffer);
		var connection = new FakeConnection(BuildNotificationBytes(EventNotification.MessageTypeIdentifier, buffer));
		var handler = new TestNotificationHandler(connection);

		// Act
		await handler.HandleNotificationAsync();

		// Assert
		Assert.Equal(notification.Topic.Count, handler.LastNotification.Topic.Count);
	}

	[Fact]
	public async Task HandleNotificationAsync_UnknownTypeByte_ThrowsFormatException()
	{
		// Arrange
		var connection = new FakeConnection(new byte[] { 0x63, 0x00, 0x00, 0x00, 0x00 });
		var handler = new TestNotificationHandler(connection);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<FormatException>(() => handler.HandleNotificationAsync());
		Assert.Contains("0x63", ex.Message);
	}

	[Fact]
	public async Task HandleNotificationAsync_TruncatedMessageSize_ThrowsInvalidDataException()
	{
		// Arrange
		var connection = new FakeConnection(new byte[] { 0x00, 0x01 });
		var handler = new TestNotificationHandler(connection);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<IOException>(() => handler.HandleNotificationAsync());
		Assert.Contains("Connection closed", ex.Message);
	}

	[Fact]
	public async Task HandleNotificationAsync_TruncatedPayload_ThrowsInvalidDataException()
	{
		// Arrange
		var notification = new EventNotification(
			new SerializableByteMemory(3, new byte[] { 1, 2, 3 }),
			new SerializableByteMemory(2, new byte[] { 4, 5 }));
		var buffer = new byte[notification.ByteSize];
		notification.ToBytes(buffer);

		var halfLen = buffer.Length / 2;
		var halfBuffer = new byte[halfLen];
		Array.Copy(buffer, halfBuffer, halfLen);
		var sizeBytes = BitConverter.GetBytes(buffer.Length);
		var data = new byte[1 + sizeBytes.Length + halfLen];
		data[0] = EventNotification.MessageTypeIdentifier;
		sizeBytes.CopyTo(data, 1);
		halfBuffer.CopyTo(data, 1 + sizeBytes.Length);
		var connection = new FakeConnection(data);
		var handler = new TestNotificationHandler(connection);

		// Act & Assert
		var ex = await Assert.ThrowsAsync<IOException>(() => handler.HandleNotificationAsync());
		Assert.Contains("Connection closed", ex.Message);
	}
}
