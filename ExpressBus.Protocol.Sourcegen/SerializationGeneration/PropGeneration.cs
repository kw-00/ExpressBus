namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

using System;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

internal static class PropGeneration
{
    public static string Generate(SerializablePropData propData)
    {
        return $"public {propData.Type.GetClrType()} {propData.Name} {{ get; }}";
    }
}

