using System;
using System.Collections.Generic;
using System.Text;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

namespace ExpressBus.Protocol.Sourcegen.Generation;

public static class ConstructorGeneration
{
    public static string Generate(string className, IReadOnlyList<SerializablePropData> propData)
    {
        if (propData.Count == 0)
        {
            return $"public {className}() {{ }}";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"public {className}(");

        for (int i = 0; i < propData.Count; i++)
        {
            var prop = propData[i];
            sb.Append($"        {GetClrType(prop.Type)} {prop.Name}");
            if (i < propData.Count - 1)
            {
                sb.Append(",");
            }
            sb.AppendLine();
        }

        sb.AppendLine("    )");
        sb.AppendLine("{");

        foreach (var prop in propData)
        {
            sb.AppendLine($"        this.{prop.Name} = {prop.Name};");
        }

        sb.AppendLine("    }");
        return sb.ToString();
    }

    private static string GetClrType(SerializablePropType type)
    {
        return type switch
        {
            SerializablePropType.Byte => "byte",
            SerializablePropType.Int => "int",
            SerializablePropType.Guid => "Guid",
            SerializablePropType.ByteMemory => "ReadOnlyMemory<byte>",
            _ => "object"
        };
    }
}
