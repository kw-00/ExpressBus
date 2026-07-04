namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Broadcast notification — pushes a message to subscribers of a topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Notification)]
[GenerateSerializedField("RequestId", typeof(Guid))]
[GenerateSerializedField("Topic", typeof(SerializableByteMemory))]
[GenerateSerializedField("Message", typeof(SerializableByteMemory))]
public readonly partial struct BroadcastNotification
{
}
