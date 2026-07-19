namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

using System;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

internal static class SerializablePropTypeExtensions
{
    public static string GetClrType(this SerializablePropType type)
    {
        return type switch
        {
            SerializablePropType.Byte => "byte",
            SerializablePropType.Int => "int",
            SerializablePropType.Guid => "Guid",
            SerializablePropType.ByteMemory => "ReadOnlyMemory<byte>",
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported serializable property type: {type}")
        };
    }
}
