using System;
using System.Threading;

namespace ExpressBus.Concurrency;

/// <summary>
/// Partitioned reader-writer lock that routes resources of type <typeparamref name="R"/>
/// to per-partition <see cref="ReaderWriterLockSlim"/> instances to reduce contention.
/// </summary>
/// <typeparam name="R">The resource key type used for partition routing.</typeparam>
public sealed class PartitionedReaderWriterLock<R> : IDisposable
{
    private readonly PartitionedProvider<R, ReaderWriterLockSlim> _provider;

    /// <summary>
    /// Creates a partitioned reader-writer lock with the specified number of partitions.
    /// </summary>
    /// <param name="partitionCount">Number of partitions (must be &gt; 0).</param>
    /// <param name="hashFunction">Function that produces a hash code for a resource key.</param>
    public PartitionedReaderWriterLock(int partitionCount, Func<R, int> hashFunction)
    {
        _provider = new PartitionedProvider<R, ReaderWriterLockSlim>(
            partitionCount, hashFunction, () => new ReaderWriterLockSlim());
    }

    /// <summary>
    /// The number of partitions.
    /// </summary>
    public int PartitionCount => _provider.PartitionCount;

    /// <summary>
    /// Acquires a read lock for the partition containing the given resource.
    /// </summary>
    public void AcquireRead(R resource)
    {
        var lockObj = _provider.Get(resource);
        lockObj.EnterReadLock();
    }

    /// <summary>
    /// Releases a read lock for the partition containing the given resource.
    /// </summary>
    public void ReleaseRead(R resource)
    {
        var lockObj = _provider.Get(resource);
        lockObj.ExitReadLock();
    }

    /// <summary>
    /// Acquires a write lock for the partition containing the given resource.
    /// </summary>
    public void AcquireWrite(R resource)
    {
        var lockObj = _provider.Get(resource);
        lockObj.EnterWriteLock();
    }

    /// <summary>
    /// Releases a write lock for the partition containing the given resource.
    /// </summary>
    public void ReleaseWrite(R resource)
    {
        var lockObj = _provider.Get(resource);
        lockObj.ExitWriteLock();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
