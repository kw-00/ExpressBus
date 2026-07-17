using System;
using System.Collections.Generic;
using System.Threading;

namespace ExpressBus.Concurrency;

/// <summary>
/// Routes objects of type <typeparamref name="R"/> to one of a fixed set of
/// <typeparamref name="L"/> instances using hash-based partitioning.
/// </summary>
/// <typeparam name="R">The routing key type (e.g., a topic identifier).</typeparam>
/// <typeparam name="L">The locked/resource type (e.g., ReaderWriterLockSlim).</typeparam>
public sealed class PartitionedProvider<R, L> : IDisposable
{
    private readonly L[] _partitions;
    private readonly Func<R, int> _hashFunction;
    private readonly ReaderWriterLockSlim _lock = new();
    private int _disposed;

    /// <summary>
    /// Creates a partitioned provider with the specified number of partitions.
    /// </summary>
    /// <param name="partitionCount">Number of partitions (must be &gt; 0).</param>
    /// <param name="hashFunction">Function that produces a hash code for a routing key.</param>
    /// <param name="producer">Factory that creates a new L for each partition index.</param>
    public PartitionedProvider(int partitionCount, Func<R, int> hashFunction, Func<L> producer)
    {
        if (partitionCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(partitionCount), "Must be greater than zero.");

        _partitions = new L[partitionCount];
        for (var i = 0; i < partitionCount; i++)
            _partitions[i] = producer();

        _hashFunction = hashFunction;
        PartitionCount = partitionCount;
    }

    /// <summary>
    /// The number of partitions.
    /// </summary>
    public int PartitionCount { get; }

    /// <summary>
    /// Returns the L instance associated with the given routing key.
    /// </summary>
    public L Get(R key)
    {
        _lock.EnterReadLock();
        try
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
                throw new ObjectDisposedException(nameof(PartitionedProvider<R, L>));

            var hash = _hashFunction(key);
            var index = hash % PartitionCount;
            if (index < 0)
                index = -index;
            return _partitions[index];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _lock.EnterWriteLock();
        try
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
                return;

            Interlocked.Exchange(ref _disposed, 1);

            foreach (var partition in _partitions)
            {
                if (partition is IDisposable disposable)
                    disposable.Dispose();
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
