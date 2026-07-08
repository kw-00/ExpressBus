namespace ExpressBus.DataStructures;

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
        return MemoryComparer<byte>.Instance.GetHashCode(key);
    }
}
