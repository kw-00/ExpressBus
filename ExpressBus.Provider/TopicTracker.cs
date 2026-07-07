using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ExpressBus.Concurrency;
using ExpressBus.Transfer;

namespace ExpressBus.Provider;

/// <summary>
/// Thread-safe tracker for topic-to-connection subscriptions.
/// </summary>
/// <remarks>
/// Topics are keyed by <see cref="ReadOnlyMemory{T}"/> and each topic maps to a set of
/// <see cref="IConnection"/> subscribers. Topics are automatically removed when their
/// subscriber count reaches zero. Uses partitioned locking to reduce contention.
/// </remarks>
public sealed class TopicTracker
{
    /// <summary>
    /// Comparer that uses reference equality for <see cref="IConnection"/> instances.
    /// </summary>
    private sealed class ConnectionReferenceComparer : IEqualityComparer<IConnection>
    {
        public static readonly ConnectionReferenceComparer Instance = new();
        private ConnectionReferenceComparer() { }
        public bool Equals(IConnection? x, IConnection? y) => ReferenceEquals(x, y);
        public int GetHashCode(IConnection? obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>
    /// Custom comparer that hashes and compares <see cref="ReadOnlyMemory{T}"/> by contents.
    /// </summary>
    private sealed class TopicKeyComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public static readonly TopicKeyComparer Instance = new();

        internal TopicKeyComparer() { }

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

    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, HashSet<IConnection>> _subscribers = new(new TopicKeyComparer());
    private readonly PartitionedReaderWriterLock<ReadOnlyMemory<byte>> _locks;
    private readonly ReaderWriterLockSlim _bulkLock = new();

    /// <summary>
    /// Creates a TopicTracker with the specified number of lock partitions.
    /// </summary>
    /// <param name="partitionCount">Number of partitions for lock distribution (default 16).</param>
    public TopicTracker(int partitionCount = 16)
    {
        _locks = new PartitionedReaderWriterLock<ReadOnlyMemory<byte>>(partitionCount, HashProducers.ForReadOnlyMemoryByte);
    }

    /// <summary>
    /// Adds a subscriber to the specified topic. Creates the topic if it does not exist.
    /// </summary>
    public void AddSubscriber(ReadOnlyMemory<byte> topic, IConnection subscriber)
    {
        _locks.AcquireWrite(topic);
        try
        {
            var set = _subscribers.GetOrAdd(topic, _ => new HashSet<IConnection>(ConnectionReferenceComparer.Instance));
            set.Add(subscriber);
        }
        finally
        {
            _locks.ReleaseWrite(topic);
        }
    }

    /// <summary>
    /// Removes a subscriber from the specified topic.
    /// </summary>
    /// <returns><c>true</c> if the subscriber was found and removed; <c>false</c> otherwise.</returns>
    public bool RemoveSubscriber(ReadOnlyMemory<byte> topic, IConnection subscriber)
    {
        _locks.AcquireWrite(topic);
        try
        {
            if (!_subscribers.TryGetValue(topic, out var set))
                return false;

            var removed = set.Remove(subscriber);
            if (removed && set.Count == 0)
                _subscribers.TryRemove(topic, out _);

            return removed;
        }
        finally
        {
            _locks.ReleaseWrite(topic);
        }
    }

    /// <summary>
    /// Removes the connection from all topics it is subscribed to.
    /// </summary>
    public void RemoveSubscriber(IConnection subscriber)
    {
        _bulkLock.EnterWriteLock();
        try
        {
            var topicsToRemove = new List<ReadOnlyMemory<byte>>();

            foreach (var kvp in _subscribers)
            {
                if (kvp.Value.Remove(subscriber))
                {
                    if (kvp.Value.Count == 0)
                        topicsToRemove.Add(kvp.Key);
                }
            }

            foreach (var topic in topicsToRemove)
                _subscribers.TryRemove(topic, out _);
        }
        finally
        {
            _bulkLock.ExitWriteLock();
        }
    }
}
