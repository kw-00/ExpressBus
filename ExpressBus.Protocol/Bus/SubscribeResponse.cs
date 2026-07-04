namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Subscribe response — confirms subscription to a topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Response)]
[GenerateSerializedField("RequestId", typeof(Guid))]
public readonly partial struct SubscribeResponse
{
}
