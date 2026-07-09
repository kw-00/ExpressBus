using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ExpressBus.Concurrency;

namespace ExpressBus.DataStructures;

/// <summary>
/// Thread-safe mapping from group keys to collections of values, using partitioned locking
/// to reduce contention on individual groups.
/// </summary>
/// <typeparam name="G">The group key type.</typeparam>
/// <typeparam name="V">The value type stored within each group.</typeparam>
/// <remarks>
/// Operations on a single group acquire a per-partition lock based on the group's hash.
/// Bulk operations (e.g. <see cref="RemoveEverywhere"/>) acquire a global bulk lock.
/// </remarks>
public sealed class Grouping<G, V> where G : notnull
{
    private readonly ConcurrentDictionary<G, HashSet<V>> _groups;
    private readonly PartitionedReaderWriterLock<G> _locks;
    private readonly ReaderWriterLockSlim _bulkLock = new();

    /// <summary>
    /// Creates a <see cref="Grouping{TKey, TValue}"/> with the specified comparer, hash function, and partition count.
    /// </summary>
    /// <param name="comparer">Comparer for group keys.</param>
    /// <param name="hashFunction">Hash function for routing group keys to partitions.</param>
    /// <param name="partitionCount">Number of lock partitions (default 16).</param>
    public Grouping(IEqualityComparer<G> comparer, Func<G, int> hashFunction, int partitionCount = 16)
    {
        _groups = new ConcurrentDictionary<G, HashSet<V>>(comparer);
        _locks = new PartitionedReaderWriterLock<G>(partitionCount, hashFunction);
    }

    /// <summary>
    /// Adds a value to the specified group. Creates the group if it does not exist.
    /// </summary>
    public void Add(G group, V value)
    {
        _locks.AcquireWrite(group);
        try
        {
            var set = _groups.GetOrAdd(group, _ => new HashSet<V>());
            set.Add(value);
        }
        finally
        {
            _locks.ReleaseWrite(group);
        }
    }

    /// <summary>
    /// Removes a value from the specified group.
    /// </summary>
    /// <returns><c>true</c> if the value was found and removed; <c>false</c> otherwise.</returns>
    public bool Remove(G group, V value)
    {
        _locks.AcquireWrite(group);
        try
        {
            if (!_groups.TryGetValue(group, out var set))
                return false;

            var removed = set.Remove(value);
            if (removed && set.Count == 0)
                _groups.TryRemove(group, out _);

            return removed;
        }
        finally
        {
            _locks.ReleaseWrite(group);
        }
    }

    /// <summary>
    /// Returns a copy of the value set for the specified group.
    /// </summary>
    /// <param name="group">The group to look up.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing copies of all values in the group,
    /// or an empty set if the group does not exist.</returns>
    public HashSet<V> Get(G group)
    {
        _locks.AcquireRead(group);
        try
        {
            if (!_groups.TryGetValue(group, out var set))
                return new HashSet<V>();

            return new HashSet<V>(set);
        }
        finally
        {
            _locks.ReleaseRead(group);
        }
    }

    /// <summary>
    /// Removes the specified group and all of its values atomically.
    /// </summary>
    /// <param name="group">The group key to remove entirely.</param>
    public void RemoveAll(G group)
    {
        _locks.AcquireWrite(group);
        try
        {
            _groups.TryRemove(group, out _);
        }
        finally
        {
            _locks.ReleaseWrite(group);
        }
    }

    /// <summary>
    /// Removes all groups and their values.
    /// </summary>
    public void Clear()
    {
        _bulkLock.EnterWriteLock();
        try
        {
            _groups.Clear();
        }
        finally
        {
            _bulkLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes the value from all groups it appears in.
    /// </summary>
    /// <param name="value">The value to remove from every group.</param>
    public void RemoveEverywhere(V value)
    {
        _bulkLock.EnterWriteLock();
        try
        {
            var groupsToRemove = new List<G>();

            foreach (var kvp in _groups)
            {
                if (kvp.Value.Remove(value))
                {
                    if (kvp.Value.Count == 0)
                        groupsToRemove.Add(kvp.Key);
                }
            }

            foreach (var group in groupsToRemove)
                _groups.TryRemove(group, out _);
        }
        finally
        {
            _bulkLock.ExitWriteLock();
        }
    }
}
