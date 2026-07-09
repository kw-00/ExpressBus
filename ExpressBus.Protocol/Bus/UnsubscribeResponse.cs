namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Unsubscribe response — confirms unsubscription from a specific topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Response)]
[GenerateSerializedProp("Status", typeof(Status))]
[GenerateSerializedProp("RequestId", typeof(Guid))]
public readonly partial struct UnsubscribeResponse : IRequestAssociated
{
}
