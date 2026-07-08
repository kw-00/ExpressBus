using System.Collections.Generic;

namespace ExpressBus.DataStructures;

/// <summary>
/// Comparer that hashes and compares <see cref="ReadOnlyMemory{T}"/> by contents.
/// </summary>
public sealed class TopicKeyComparer : IEqualityComparer<ReadOnlyMemory<byte>>
{
    public static readonly TopicKeyComparer Instance = new();

    private TopicKeyComparer() { }

    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
    {
        if (x.Length != y.Length)
            return false;
        return x.Span.SequenceEqual(y.Span);
    }

    public int GetHashCode(ReadOnlyMemory<byte> obj)
    {
        var span = obj.Span;
        var hc = new System.HashCode();
        for (var i = 0; i < span.Length; i++)
            hc.Add(span[i]);
        return hc.ToHashCode();
    }
}
