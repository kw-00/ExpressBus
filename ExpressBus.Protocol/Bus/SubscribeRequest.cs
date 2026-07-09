namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Subscribe request — asks to subscribe to a topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedProp("RequestId", typeof(Guid))]
[GenerateSerializedProp("Topic", typeof(SerializableByteMemory))]
public readonly partial struct SubscribeRequest : IRequestAssociated
{
}
