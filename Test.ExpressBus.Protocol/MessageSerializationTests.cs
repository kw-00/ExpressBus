using System.Runtime.InteropServices;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Protocol.SourcegenTesting;
using BusStatus = ExpressBus.Protocol.Bus.Status;

public class MessageSerializationTests
{
	[Fact]
	public void TestMessage_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var original = new TestMessage(
			42,
			123456,
			new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = TestMessage.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Id, deserialized.Id);
		Assert.Equal(original.Sequence, deserialized.Sequence);
		Assert.Equal(original.CorrelationId, deserialized.CorrelationId);
	}

	[Fact]
	public void SimpleMessage_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var original = new SimpleMessage(-32768, 3.14159265);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = SimpleMessage.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Value, deserialized.Value);
		Assert.Equal(original.Ratio, deserialized.Ratio);
	}

	[Fact]
	public void EmptyMessage_Serialize_Deserialize_PreservesMessage()
	{
		// Arrange
		var original = new EmptyMessage();

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = EmptyMessage.FromBytes(buffer);

		// Assert — empty struct has no fields, roundtrip is trivially correct
	}

	[Theory]
	[InlineData(typeof(TestMessage), 22)]
	[InlineData(typeof(SimpleMessage), 11)]
	[InlineData(typeof(EmptyMessage), 1)]
	public void ByteSize_IsCorrect(Type type, int expected)
	{
		var flags = System.Reflection.BindingFlags.Public
			| System.Reflection.BindingFlags.Instance
			| System.Reflection.BindingFlags.NonPublic;
		var sizeProp = type.GetProperty("ByteSize", flags)!;
		// Create a default instance to read ByteSize
		var instance = System.Activator.CreateInstance(type)!;
		var actual = (int)(sizeProp.GetValue(instance) ?? throw new InvalidOperationException(
			$"ByteSize property is null on {type.FullName}"));
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void FromBytes_ThrowsOnTooShortBuffer()
	{
		// Arrange
		var original = new TestMessage(
			0,
			0,
			Guid.Empty);
		var shortBuffer = new byte[original.ByteSize - 1];

		// Act & Assert
		Assert.Throws<FormatException>(() => TestMessage.FromBytes(shortBuffer));
	}

	[Fact]
	public void FromBytes_ThrowsOnTooLongBuffer()
	{
		// Arrange
		var original = new TestMessage(
			0,
			0,
			Guid.Empty);
		var longBuffer = new byte[original.ByteSize + 5];

		// Act & Assert
		Assert.Throws<FormatException>(() => TestMessage.FromBytes(longBuffer));
	}

	[Fact]
	public void MessageTypeIdentifier_IsUniquePerType()
	{
		// Act
		var testId = GetMessageTypeIdentifier<TestMessage>();
		var simpleId = GetMessageTypeIdentifier<SimpleMessage>();
		var emptyId = GetMessageTypeIdentifier<EmptyMessage>();

		// Assert
		var ids = new[] { testId, simpleId, emptyId };
		Assert.Equal(ids.Distinct().Count(), ids.Length);
	}

	private static byte GetMessageTypeIdentifier<T>() where T : struct
	{
		var flags = System.Reflection.BindingFlags.Public
			| System.Reflection.BindingFlags.Static
			| System.Reflection.BindingFlags.NonPublic;
		var prop = typeof(T).GetProperty("MessageTypeIdentifier", flags)!;
		
		return (byte)(prop.GetValue(null) ?? throw new InvalidOperationException(
			$"MessageTypeIdentifier property is null on {typeof(T).FullName}"));
	}

	[Fact]
	public void ByteEnumMessage_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var original = new ByteEnumMessage(Priority.High, 999);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = ByteEnumMessage.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Priority, deserialized.Priority);
		Assert.Equal(original.Sequence, deserialized.Sequence);
	}

	[Fact]
	public void IntEnumMessage_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var original = new IntEnumMessage(ExpressBus.Protocol.SourcegenTesting.Status.Failed, 123456789L);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = IntEnumMessage.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Status, deserialized.Status);
		Assert.Equal(original.Timestamp, deserialized.Timestamp);
	}

	[Fact]
	public void LongEnumMessage_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var original = new LongEnumMessage(Operation.Delete, 255);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = LongEnumMessage.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Operation, deserialized.Operation);
		Assert.Equal(original.Id, deserialized.Id);
	}

	[Theory]
	[InlineData(typeof(ByteEnumMessage), 6)]
	[InlineData(typeof(IntEnumMessage), 13)]
	[InlineData(typeof(LongEnumMessage), 10)]
	public void EnumMessage_ByteSize_IsCorrect(Type type, int expected)
	{
		var flags = System.Reflection.BindingFlags.Public
			| System.Reflection.BindingFlags.Instance
			| System.Reflection.BindingFlags.NonPublic;
		var sizeProp = type.GetProperty("ByteSize", flags)!;
		var instance = System.Activator.CreateInstance(type)!;
		var actual = (int)(sizeProp.GetValue(instance) ?? throw new InvalidOperationException(
			$"ByteSize property is null on {type.FullName}"));
		Assert.Equal(expected, actual);
	}

	private static bool IsRequestMessage(Type t)
	{
		if (!(t.IsValueType && !t.IsAbstract))
			return false;
		var attrs = t.GetCustomAttributes(typeof(MessageAttribute), false);
		if (attrs.Length == 0)
			return false;
		return ((MessageAttribute)attrs[0]).Type == MessageType.Request;
	}

	// --- SerializableMemory tests ---

	[Fact]
	public void SerializableByteMemory_Serialize_Deserialize_PreservesData()
	{
		// Arrange
		var data = new byte[] { 1, 2, 3, 4, 5 };
		var original = new SerializableByteMemory(data.Length, data);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = SerializableByteMemory.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Count, deserialized.Count);
		Assert.Equal(original.Data.ToArray(), deserialized.Data.ToArray());
	}

	[Fact]
	public void SerializableIntMemory_Serialize_Deserialize_PreservesData()
	{
		// Arrange
		var data = new int[] { 42, -17, 0, 1000 };
		var rawData = MemoryMarshal.AsBytes(data.AsSpan()).ToArray();
		var original = new SerializableIntMemory(data.Length, rawData);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = SerializableIntMemory.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Count, deserialized.Count);
		var deserializedInts = MemoryMarshal.Cast<byte, int>(deserialized.Data.ToArray()).ToArray();
		Assert.Equal(data, deserializedInts);
	}

	[Fact]
	public void SerializableLongMemory_Serialize_Deserialize_PreservesData()
	{
		// Arrange
		var data = new long[] { 9223372036854775807L, -9223372036854775808L, 0L };
		var rawData = MemoryMarshal.AsBytes(data.AsSpan()).ToArray();
		var original = new SerializableLongMemory(data.Length, rawData);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = SerializableLongMemory.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Count, deserialized.Count);
		var deserializedLongs = MemoryMarshal.Cast<byte, long>(deserialized.Data.ToArray()).ToArray();
		Assert.Equal(data, deserializedLongs);
	}

	[Fact]
	public void SerializableBoolMemory_Serialize_Deserialize_PreservesData()
	{
		// Arrange
		var data = new bool[] { true, false, true, true, false };
		var rawData = MemoryMarshal.AsBytes(data.AsSpan()).ToArray();
		var original = new SerializableBoolMemory(data.Length, rawData);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = SerializableBoolMemory.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Count, deserialized.Count);
		var deserializedBools = MemoryMarshal.Cast<byte, bool>(deserialized.Data.ToArray()).ToArray();
		Assert.Equal(data, deserializedBools);
	}

	[Fact]
	public void SerializableGuidMemory_Serialize_Deserialize_PreservesData()
	{
		// Arrange
		var guid = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
		var data = new Guid[] { guid };
		var rawData = MemoryMarshal.AsBytes(data.AsSpan()).ToArray();
		var original = new SerializableGuidMemory(data.Length, rawData);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = SerializableGuidMemory.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Count, deserialized.Count);
		var deserializedGuids = MemoryMarshal.Cast<byte, Guid>(deserialized.Data.ToArray()).ToArray();
		Assert.Equal(guid, deserializedGuids[0]);
	}

	[Theory]
	[InlineData(typeof(SerializableByteMemory), 5)]
	[InlineData(typeof(SerializableBoolMemory), 5)]
	[InlineData(typeof(SerializableIntMemory), 5)]
	[InlineData(typeof(SerializableLongMemory), 5)]
	[InlineData(typeof(SerializableGuidMemory), 5)]
	public void SerializableMemory_ByteSize_IsCorrect(Type type, int expected)
	{
		var flags = System.Reflection.BindingFlags.Public
			| System.Reflection.BindingFlags.Instance
			| System.Reflection.BindingFlags.NonPublic;
		var sizeProp = type.GetProperty("ByteSize", flags)!;
		var instance = System.Activator.CreateInstance(type)!;
		var actual = (int)(sizeProp.GetValue(instance) ?? throw new InvalidOperationException(
			$"ByteSize property is null on {type.FullName}"));
		Assert.Equal(expected, actual);
	}

	// --- Bus message tests ---

	[Fact]
	public void SubscribeRequest_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(5, new byte[] { 1, 2, 3, 4, 5 });
		var original = new SubscribeRequest(requestId, topic);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = SubscribeRequest.FromBytes(buffer);

		// Assert
		Assert.Equal(original.RequestId, deserialized.RequestId);
		Assert.Equal(original.Topic.Count, deserialized.Topic.Count);
		Assert.Equal(original.Topic.Data.ToArray(), deserialized.Topic.Data.ToArray());
	}

	[Fact]
	public void SubscribeResponse_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var status = BusStatus.Success;
		var original = new SubscribeResponse(status, requestId);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = SubscribeResponse.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Status, deserialized.Status);
		Assert.Equal(original.RequestId, deserialized.RequestId);
	}

	[Fact]
	public void BroadcastRequest_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var topic = new SerializableByteMemory(4, new byte[] { 97, 98, 99, 100 });
		var message = new SerializableByteMemory(3, new byte[] { 10, 20, 30 });
		var original = new BroadcastRequest(requestId, topic, message);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = BroadcastRequest.FromBytes(buffer);

		// Assert
		Assert.Equal(original.RequestId, deserialized.RequestId);
		Assert.Equal(original.Topic.Count, deserialized.Topic.Count);
		Assert.Equal(original.Message.Count, deserialized.Message.Count);
	}

	[Fact]
	public void BroadcastResponse_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var status = BusStatus.Success;
		var original = new BroadcastResponse(status, requestId);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = BroadcastResponse.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Status, deserialized.Status);
		Assert.Equal(original.RequestId, deserialized.RequestId);
	}

	[Fact]
	public void EventNotification_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var topic = new SerializableByteMemory(6, new byte[] { 1, 2, 3, 4, 5, 6 });
		var message = new SerializableByteMemory(2, new byte[] { 7, 8 });
		var original = new EventNotification(topic, message);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = EventNotification.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Topic.Count, deserialized.Topic.Count);
		Assert.Equal(original.Message.Count, deserialized.Message.Count);
	}

	[Theory]
	[InlineData(typeof(SubscribeRequest), 22)] // MessageType(1) + Guid(16) + Topic(5 for 0 items)
	[InlineData(typeof(SubscribeResponse), 18)] // MessageType(1) + Status(1) + Guid(16)
	[InlineData(typeof(BroadcastRequest), 27)] // MessageType(1) + Guid(16) + Topic(5) + Message(5)
	[InlineData(typeof(BroadcastResponse), 18)] // MessageType(1) + Status(1) + Guid(16)
	[InlineData(typeof(EventNotification), 11)] // MessageType(1) + Topic(5) + Message(5)
	public void BusMessage_ByteSize_HasCorrectMinimum(Type type, int expectedMinimum)
	{
		var flags = System.Reflection.BindingFlags.Public
			| System.Reflection.BindingFlags.Instance
			| System.Reflection.BindingFlags.NonPublic;
		var sizeProp = type.GetProperty("ByteSize", flags)!;
		var instance = System.Activator.CreateInstance(type)!;
		var actual = (int)(sizeProp.GetValue(instance) ?? throw new InvalidOperationException(
			$"ByteSize property is null on {type.FullName}"));
		Assert.Equal(expectedMinimum, actual);
	}

	// --- Unsubscribe message tests ---

	[Fact]
	public void UnsubscribeRequest_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var status = BusStatus.Success;
		var topic = new SerializableByteMemory(4, new byte[] { 97, 98, 99, 100 });
		var original = new UnsubscribeRequest(status, requestId, topic);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = UnsubscribeRequest.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Status, deserialized.Status);
		Assert.Equal(original.RequestId, deserialized.RequestId);
		Assert.Equal(original.Topic.Count, deserialized.Topic.Count);
		Assert.Equal(original.Topic.Data.ToArray(), deserialized.Topic.Data.ToArray());
	}

	[Fact]
	public void UnsubscribeAllRequest_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var status = BusStatus.Error;
		var original = new UnsubscribeAllRequest(status, requestId);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = UnsubscribeAllRequest.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Status, deserialized.Status);
		Assert.Equal(original.RequestId, deserialized.RequestId);
	}

	[Fact]
	public void UnsubscribeResponse_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var status = BusStatus.Success;
		var original = new UnsubscribeResponse(status, requestId);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = UnsubscribeResponse.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Status, deserialized.Status);
		Assert.Equal(original.RequestId, deserialized.RequestId);
	}

	[Fact]
	public void UnsubscribeAllResponse_Serialize_Deserialize_PreservesFields()
	{
		// Arrange
		var requestId = Guid.NewGuid();
		var status = BusStatus.Error;
		var original = new UnsubscribeAllResponse(status, requestId);

		// Act
		var buffer = new byte[original.ByteSize];
		original.ToBytes(buffer);
		var deserialized = UnsubscribeAllResponse.FromBytes(buffer);

		// Assert
		Assert.Equal(original.Status, deserialized.Status);
		Assert.Equal(original.RequestId, deserialized.RequestId);
	}

	[Theory]
	[InlineData(typeof(UnsubscribeRequest), 23)] // MessageType(1) + Status(1) + Guid(16) + Topic(5 for 0 items)
	[InlineData(typeof(UnsubscribeAllRequest), 18)] // MessageType(1) + Status(1) + Guid(16)
	[InlineData(typeof(UnsubscribeResponse), 18)] // MessageType(1) + Status(1) + Guid(16)
	[InlineData(typeof(UnsubscribeAllResponse), 18)] // MessageType(1) + Status(1) + Guid(16)
	public void UnsubscribeMessage_ByteSize_HasCorrectMinimum(Type type, int expectedMinimum)
	{
		var flags = System.Reflection.BindingFlags.Public
			| System.Reflection.BindingFlags.Instance
			| System.Reflection.BindingFlags.NonPublic;
		var sizeProp = type.GetProperty("ByteSize", flags)!;
		var instance = System.Activator.CreateInstance(type)!;
		var actual = (int)(sizeProp.GetValue(instance) ?? throw new InvalidOperationException(
			$"ByteSize property is null on {type.FullName}"));
		Assert.Equal(expectedMinimum, actual);
	}
}
