using System.Collections.Generic;
using System.Threading;
using ExpressBus.DataStructures;
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
    private readonly Grouping<ReadOnlyMemory<byte>, IConnection> _subscribers;
    private readonly ReaderWriterLockSlim _bulkLock = new();

    /// <summary>
    /// Creates a TopicTracker with the specified number of lock partitions.
    /// </summary>
    /// <param name="partitionCount">Number of partitions for lock distribution (default 16).</param>
    public TopicTracker(int partitionCount = 16)
    {
        _subscribers = new Grouping<ReadOnlyMemory<byte>, IConnection>(
            MemoryComparer<byte>.Instance,
            HashProducers.ForReadOnlyMemoryByte,
            partitionCount);
    }

    /// <summary>
    /// Adds a subscriber to the specified topic. Creates the topic if it does not exist.
    /// </summary>
    public void AddSubscriber(ReadOnlyMemory<byte> topic, IConnection subscriber)
    {
        _subscribers.Add(topic, subscriber);
    }

    /// <summary>
    /// Removes a subscriber from the specified topic.
    /// </summary>
    /// <returns><c>true</c> if the subscriber was found and removed; <c>false</c> otherwise.</returns>
    public bool RemoveSubscriber(ReadOnlyMemory<byte> topic, IConnection subscriber)
    {
        return _subscribers.Remove(topic, subscriber);
    }

    /// <summary>
    /// Removes the connection from all topics it is subscribed to.
    /// </summary>
    public void RemoveSubscriber(IConnection subscriber)
    {
        _bulkLock.EnterWriteLock();
        try
        {
            _subscribers.RemoveEverywhere(subscriber);
        }
        finally
        {
            _bulkLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns a copy of the subscriber set for the specified topic.
    /// </summary>
    /// <param name="topic">The topic to look up.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing copies of all subscriber connections,
    /// or an empty set if the topic has no subscribers.</returns>
    public HashSet<IConnection> GetSubscribers(ReadOnlyMemory<byte> topic)
    {
        return _subscribers.Get(topic);
    }

    /// <summary>
    /// Removes all subscribers from all topics.
    /// </summary>
    public void ClearAll()
    {
        _bulkLock.EnterWriteLock();
        try
        {
            _subscribers.Clear();
        }
        finally
        {
            _bulkLock.ExitWriteLock();
        }
    }
}
