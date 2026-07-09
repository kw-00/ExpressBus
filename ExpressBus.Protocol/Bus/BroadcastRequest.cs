namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Broadcast request — sends a message to a topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedProp("RequestId", typeof(Guid))]
[GenerateSerializedProp("Topic", typeof(SerializableByteMemory))]
[GenerateSerializedProp("Message", typeof(SerializableByteMemory))]
public readonly partial struct BroadcastRequest : IRequestAssociated
{
}
