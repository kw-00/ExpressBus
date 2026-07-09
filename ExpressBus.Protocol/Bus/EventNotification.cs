namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Event notification — pushes a message to subscribers of a topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Notification)]
[GenerateSerializedProp("Topic", typeof(SerializableByteMemory))]
[GenerateSerializedProp("Message", typeof(SerializableByteMemory))]
public readonly partial struct EventNotification
{
}
