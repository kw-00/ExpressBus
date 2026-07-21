using System;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

namespace ExpressBus.Protocol.Messages;

[GenerateTypeId]
[GenerateSerialization]
[GenerateSerializedProp("Topic", SerializablePropType.ByteMemory)]
[GenerateSerializedProp("Message", SerializablePropType.ByteMemory)]
public readonly ref struct BroadcastNotification
{
}
