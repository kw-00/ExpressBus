namespace ExpressBus.Protocol.Sourcegen.Generation;

using System;
using System.Collections.Generic;
using System.Text;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

public static class FromBytesGeneration
{
    public static string Generate(string className, IReadOnlyList<SerializablePropData> props)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"public static {className} FromBytes(Span<byte> buffer)");
        sb.AppendLine("{");
        sb.AppendLine($"    var reader = new ByteReader(buffer);");
        sb.Append($"    return new {className}(");
        
        for (int i = 0; i < props.Count; i++)
        {
            sb.Append(RWCallGeneration.GenerateReadCall("reader", props[i].Type));
            if (i < props.Count - 1)
            {
                sb.Append(", ");
            }
        }
        
        sb.AppendLine(");");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
