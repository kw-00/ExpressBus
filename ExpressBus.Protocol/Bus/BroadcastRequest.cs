namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Broadcast request — sends a message to a topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedField("RequestId", typeof(Guid))]
[GenerateSerializedField("Topic", typeof(SerializableByteMemory))]
[GenerateSerializedField("Message", typeof(SerializableByteMemory))]
public readonly partial struct BroadcastRequest
{
}
