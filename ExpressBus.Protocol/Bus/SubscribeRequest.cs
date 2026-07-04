namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Subscribe request — asks to subscribe to a topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedField("RequestId", typeof(Guid))]
[GenerateSerializedField("Topic", typeof(SerializableByteMemory))]
public readonly partial struct SubscribeRequest
{
}
