namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Unsubscribe request — removes a subscription from a specific topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedField("Status", typeof(Status))]
[GenerateSerializedField("RequestId", typeof(Guid))]
[GenerateSerializedField("Topic", typeof(SerializableByteMemory))]
public readonly partial struct UnsubscribeRequest
{
}
