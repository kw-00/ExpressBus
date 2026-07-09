namespace ExpressBus.Protocol.SourcegenTesting;

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedProp("Id", typeof(byte))]
[GenerateSerializedProp("Sequence", typeof(int))]
[GenerateSerializedProp("CorrelationId", typeof(Guid))]
public readonly partial struct TestMessage
{
}

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedProp("Value", typeof(short))]
[GenerateSerializedProp("Ratio", typeof(double))]
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
[GenerateSerializedProp("Priority", typeof(Priority))]
[GenerateSerializedProp("Sequence", typeof(int))]
public readonly partial struct ByteEnumMessage
{
}

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedProp("Status", typeof(Status))]
[GenerateSerializedProp("Timestamp", typeof(long))]
public readonly partial struct IntEnumMessage
{
}

[Message(MessageType.Test)]
[GenerateSerialization(MessageType.Test)]
[GenerateSerializedProp("Operation", typeof(Operation))]
[GenerateSerializedProp("Id", typeof(byte))]
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
