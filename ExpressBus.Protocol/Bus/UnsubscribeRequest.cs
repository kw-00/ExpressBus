namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Unsubscribe request — removes a subscription from a specific topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedProp("RequestId", typeof(Guid))]
[GenerateSerializedProp("Topic", typeof(SerializableByteMemory))]
public readonly partial struct UnsubscribeRequest : IRequestAssociated
{
}
