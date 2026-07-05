namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Unsubscribe-all response — confirms all topic subscriptions have been removed.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Response)]
[GenerateSerializedField("Status", typeof(Status))]
[GenerateSerializedField("RequestId", typeof(Guid))]
public readonly partial struct UnsubscribeAllResponse
{
}
