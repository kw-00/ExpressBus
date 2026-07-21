using System;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

namespace ExpressBus.Protocol.Messages;

[GenerateTypeId]
[GenerateSerialization]
[GenerateSerializedProp("RequestId", SerializablePropType.Guid)]
public readonly ref struct BroadcastResponse
{
}
