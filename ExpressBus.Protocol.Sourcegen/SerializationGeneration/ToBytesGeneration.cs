namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

using System;
using System.Collections.Generic;
using System.Text;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

internal static class ToBytesGeneration
{
    public static string Generate(string className, IReadOnlyList<SerializablePropData> props)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"public void ToBytes(Span<byte> buffer)");
        sb.AppendLine("{");
        sb.AppendLine($"    var writer = new ByteWriter(buffer);");
        foreach (var prop in props)
        {
            sb.AppendLine($"    {RWCallGeneration.GenerateWriteCall("writer", prop)}");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }
}
