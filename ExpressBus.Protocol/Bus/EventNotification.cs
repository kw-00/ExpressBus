namespace ExpressBus.Protocol.Bus;

/// <summary>
/// Event notification — pushes a message to subscribers of a topic.
/// </summary>
[Message]
[GenerateSerialization(MessageType.Notification)]
[GenerateSerializedField("Topic", typeof(SerializableByteMemory))]
[GenerateSerializedField("Message", typeof(SerializableByteMemory))]
public readonly partial struct EventNotification
{
}
