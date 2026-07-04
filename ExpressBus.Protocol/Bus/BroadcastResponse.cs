namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Broadcast response — confirms message was broadcast.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Response)]
[GenerateSerializedField("RequestId", typeof(Guid))]
public readonly partial struct BroadcastResponse
{
}
