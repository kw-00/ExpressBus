using System.Buffers;

namespace ExpressBus.Buffering;

/// <summary>
/// Wraps a rented <see cref="ArrayPool{T}"/> buffer and exposes a bounded, writable view of its memory.
/// </summary>
/// <remarks>
/// <para>
/// <c>DisposableMemory</c> exists to avoid heap allocation via boxing. <see cref="IMemoryOwner{T}"/> is an interface,
/// and passing it through value-type APIs would box the instance. This struct wraps the rented array directly,
/// exposing only a bounded <see cref="Memory"/> field that is sliced to the exact requested length,
/// preventing callers from reading or writing beyond the bounded region.
/// </para>
/// <para>
/// The underlying buffer is returned to its pool when this instance is disposed.
/// This type is a <c>readonly struct</c> so it can be returned from methods without allocation,
/// while still providing deterministic cleanup of pooled memory.
/// </para>
/// </remarks>
public readonly struct DisposableMemory : IMemoryOwner<byte>
{
    private readonly byte[] _buffer;
    private readonly ArrayPool<byte> _pool;
    private readonly int _size;

    /// <summary>
    /// A bounded, writable view of the underlying memory, sliced to the exact size
    /// specified in the constructor.
    /// </summary>
    public readonly Memory<byte> Memory;

    /// <summary>
    /// Creates a new <see cref="DisposableMemory"/> that rents a buffer from the specified pool.
    /// </summary>
    /// <param name="pool">The array pool to rent from.</param>
    /// <param name="size">The number of bytes to rent.</param>
    public DisposableMemory(ArrayPool<byte> pool, int size)
    {
        _pool = pool;
        _buffer = pool.Rent(size);
        _size = size;
        Memory = _buffer.AsMemory(0, _size);
    }

    /// <summary>
    /// Creates a new <see cref="DisposableMemory"/> that rents a buffer from <see cref="ArrayPool{T}.Shared"/>.
    /// </summary>
    /// <param name="size">The number of bytes to rent.</param>
    public DisposableMemory(int size)
        : this(ArrayPool<byte>.Shared, size)
    {
    }

    /// <summary>
    /// The full underlying memory owned by this instance (unbounded).
    /// </summary>
    Memory<byte> IMemoryOwner<byte>.Memory => _buffer.AsMemory();

    /// <summary>
    /// Disposes the underlying memory, returning the buffer to its pool.
    /// </summary>
    public void Dispose()
    {
        _pool.Return(_buffer);
    }
}
