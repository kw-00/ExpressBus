using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

internal static class FromBytesGeneration
{
    public static string Generate(string className, IReadOnlyList<SerializablePropData> props)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"public static {className} FromBytes(Span<byte> buffer)");
        sb.AppendLine("{");
        sb.AppendLine($"    var reader = new ByteReader(buffer);");

        var calls = props.Select(p => RWCallGeneration.GenerateReadCall("reader", p.Type));
        string joinedCalls = string.Join(",\n    ", calls);
        sb.AppendLine($"    return new {className}(\n    {joinedCalls});");


        sb.AppendLine("}");
        return sb.ToString();
    }
}
