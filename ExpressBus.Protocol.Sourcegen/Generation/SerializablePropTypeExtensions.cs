namespace ExpressBus.Protocol.Sourcegen.Generation;

using ExpressBus.Protocol.Sourcegen.SharedDependencies;

public static class SerializablePropTypeExtensions
{
    public static string GetClrType(this SerializablePropType type)
    {
        return type switch
        {
            SerializablePropType.Byte => "byte",
            SerializablePropType.Int => "int",
            SerializablePropType.Guid => "Guid",
            SerializablePropType.ByteMemory => "ReadOnlyMemory<byte>"
        };
    }
}
