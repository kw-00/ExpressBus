namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Unsubscribe-all request — removes all topic subscriptions at once.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedField("Status", typeof(Status))]
[GenerateSerializedField("RequestId", typeof(Guid))]
public readonly partial struct UnsubscribeAllRequest
{
}
