namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

using System;
using System.Collections.Generic;
using System.Text;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

// TODO: make this class generate source code and rename it accordingly
public static class SerializationGeneration
{
    private static readonly string[] _lineSeparators = { "\r\n", "\r", "\n" };

    private static string Indent(string content)
    {
        if (string.IsNullOrEmpty(content)) return content;
        var sb = new StringBuilder();
        var lines = content.Split(_lineSeparators, StringSplitOptions.None);
        foreach (var line in lines)
        {
            sb.AppendLine("    " + line);
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    public static string Generate(string className, IReadOnlyList<SerializablePropData> propData)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"public partial class {className} : ISerializable<{className}>");
        sb.AppendLine("{");

        // Properties
        foreach (var prop in propData)
        {
            sb.AppendLine(Indent(PropGeneration.Generate(prop)));
        }
        sb.AppendLine();

        // Constructor
        sb.AppendLine(Indent(ConstructorGeneration.Generate(className, propData)));
        sb.AppendLine();

        // ByteCount
        sb.AppendLine(Indent(ByteCountGeneration.Generate(propData)));
        sb.AppendLine();

        // ToBytes
        sb.AppendLine(Indent(ToBytesGeneration.Generate(className, propData)));
        sb.AppendLine();

        // FromBytes
        sb.AppendLine(Indent(FromBytesGeneration.Generate(className, propData)));
        sb.AppendLine("}");

        return sb.ToString();
    }
}

