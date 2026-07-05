namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Unsubscribe response — confirms unsubscription from a specific topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Response)]
[GenerateSerializedField("Status", typeof(Status))]
[GenerateSerializedField("RequestId", typeof(Guid))]
public readonly partial struct UnsubscribeResponse
{
}
