using System.Collections.Generic;

namespace ExpressBus.DataStructures;

/// <summary>
/// Comparer that hashes and compares <see cref="ReadOnlyMemory{T}"/> by contents.
/// </summary>
/// <typeparam name="T">The element type. Must be an unmanaged type.</typeparam>
public sealed class MemoryComparer<T> : IEqualityComparer<ReadOnlyMemory<T>>
    where T : unmanaged
{
    public static readonly MemoryComparer<T> Instance = new();

    private MemoryComparer() { }

    public bool Equals(ReadOnlyMemory<T> x, ReadOnlyMemory<T> y)
    {
        if (x.Length != y.Length)
            return false;
        return x.Span.SequenceEqual(y.Span);
    }

    public int GetHashCode(ReadOnlyMemory<T> obj)
    {
        var span = obj.Span;
        var hc = new System.HashCode();
        for (var i = 0; i < span.Length; i++)
            hc.Add(span[i]);
        return hc.ToHashCode();
    }
}
