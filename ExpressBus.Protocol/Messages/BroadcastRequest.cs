using System;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

namespace ExpressBus.Protocol.Messages;

[GenerateTypeId]
[GenerateSerialization]
[GenerateSerializedProp("RequestId", SerializablePropType.Guid)]
[GenerateSerializedProp("Topic", SerializablePropType.ByteMemory)]
[GenerateSerializedProp("Message", SerializablePropType.ByteMemory)]
public readonly partial struct BroadcastRequest
{
}
