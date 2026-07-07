namespace ExpressBus.Concurrency;

/// <summary>
/// Pre-built hash functions for common key types used by <see cref="PartitionedProvider{TKey, TValue}"/>.
/// </summary>
public static class HashProducers
{
    /// <summary>
    /// Hashes a <see cref="ReadOnlyMemory{T}"/> by iterating over its bytes.
    /// </summary>
    public static int ForReadOnlyMemoryByte(ReadOnlyMemory<byte> key)
    {
        var span = key.Span;
        var hc = new System.HashCode();
        for (var i = 0; i < span.Length; i++)
            hc.Add(span[i]);
        return hc.ToHashCode();
    }
}
