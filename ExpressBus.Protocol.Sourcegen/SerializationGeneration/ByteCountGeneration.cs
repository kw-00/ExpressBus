using System;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;
using System.Collections.Generic;
using System.Linq;

namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

internal static class ByteCountGeneration
{
    public static string Generate(IEnumerable<SerializablePropData> props)
    {
        var parts = props.Select(prop => prop.Type switch
        {
            SerializablePropType.Byte => "1",
            SerializablePropType.Int => "4",
            SerializablePropType.Guid => "16",
            SerializablePropType.ByteMemory => $"4 + {prop.Name}.Length",
            _ => throw new ArgumentOutOfRangeException(nameof(prop.Type), prop.Type, $"Unsupported serializable property type: {prop.Type}")
        }).ToList();

        if (parts.Count == 0)
        {
            return "public int ByteCount => 0;";
        }

        return $"public int ByteCount => {string.Join(" + ", parts)};";
    }
}
