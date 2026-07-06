using System.Buffers;

namespace ExpressBus.Buffering;

/// <summary>
/// Wraps an <see cref="IMemoryOwner{T}"/> and exposes only a bounded, read-only view of its memory.
/// </summary>
/// <remarks>
/// <para>
/// When memory is rented from a pool (e.g., <see cref="MemoryPool{T}.Shared"/>), the returned
/// buffer is typically larger than the requested size. <c>DisposableMemory</c> solves this by
/// wrapping the owner and exposing a <see cref="Memory"/> property that is sliced to the exact
/// requested length, preventing callers from writing beyond the bounded region.
/// They dispose the wrapper via <see cref="IDisposable.Dispose"/> instead.
/// </para>
/// <para>
/// This type is a <c>readonly struct</c> so it can be returned from methods without allocation,
/// while still providing deterministic cleanup of pooled memory.
/// </para>
/// </remarks>
public readonly struct DisposableMemory : IDisposable
{
    private readonly IMemoryOwner<byte> _owner;
    private readonly int _size;

    /// <summary>
    /// Creates a new <see cref="DisposableMemory"/> wrapping the given owner with a bounded view.
    /// </summary>
    /// <param name="owner">The memory owner to wrap. Must not be null.</param>
    /// <param name="size">The exact number of bytes of <paramref name="owner"/> that should be
    /// exposed via the <see cref="Memory"/> property.</param>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative or greater
    /// than <paramref name="owner"/>.Memory.Length.</exception>
    public DisposableMemory(IMemoryOwner<byte> owner, int size)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _size = size;

        if (size < 0 || size > _owner.Memory.Length)
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                $"Size must be between 0 and {_owner.Memory.Length}.");
    }

    /// <summary>
    /// A read-only, bounded view of the underlying memory, sliced to the exact size
    /// specified in the constructor.
    /// </summary>
    public ReadOnlyMemory<byte> Memory => _owner.Memory.Slice(0, _size);

    /// <summary>
    /// Disposes the underlying memory owner, returning the buffer to its pool.
    /// </summary>
    public void Dispose()
    {
        _owner?.Dispose();
    }
}
