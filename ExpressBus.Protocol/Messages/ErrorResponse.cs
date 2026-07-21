using System;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

namespace ExpressBus.Protocol.Messages;

[GenerateTypeId]
[GenerateSerialization]
[GenerateSerializedProp("RequestId", SerializablePropType.Guid)]
[GenerateSerializedProp("ErrorCode", SerializablePropType.Int)]
public readonly ref struct ErrorResponse
{
}
