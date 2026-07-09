namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Unsubscribe-all request — removes all topic subscriptions at once.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedProp("RequestId", typeof(Guid))]
public readonly partial struct UnsubscribeAllRequest : IRequestAssociated
{
}
