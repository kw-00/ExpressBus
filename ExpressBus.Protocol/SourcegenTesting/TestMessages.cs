namespace ExpressBus.Protocol.SourcegenTesting;

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedField("Id", typeof(byte))]
[GenerateSerializedField("Sequence", typeof(int))]
[GenerateSerializedField("CorrelationId", typeof(Guid))]
public readonly partial struct TestMessage
{
}

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedField("Value", typeof(short))]
[GenerateSerializedField("Ratio", typeof(double))]
public readonly partial struct SimpleMessage
{
}

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
public readonly partial struct EmptyMessage
{
}

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedField("Priority", typeof(Priority))]
[GenerateSerializedField("Sequence", typeof(int))]
public readonly partial struct ByteEnumMessage
{
}

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedField("Status", typeof(Status))]
[GenerateSerializedField("Timestamp", typeof(long))]
public readonly partial struct IntEnumMessage
{
}

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedField("Operation", typeof(Operation))]
[GenerateSerializedField("Id", typeof(byte))]
public readonly partial struct LongEnumMessage
{
}

public enum Priority : byte
{
	Low,
	Medium,
	High,
	Critical
}

public enum Status : int
{
	Pending,
	Active,
	Completed,
	Failed
}

public enum Operation : long
{
	Read,
	Write,
	Delete,
	Update
}
