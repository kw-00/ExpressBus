using System.Collections.Concurrent;

namespace ExpressBus.Client;

/// <summary>
/// Thread-safe registry of topic-to-handler delegates for incoming event notifications.
/// </summary>
/// <remarks>
/// Topics are keyed by <see cref="ReadOnlyMemory{T}"/> contents and each topic maps to a single
/// <see cref="Action{T}"/> handler that receives the notification message payload.
/// </remarks>
public sealed class EventHandlers
{
    /// <summary>
    /// Custom comparer that hashes and compares <see cref="ReadOnlyMemory{T}"/> by contents.
    /// </summary>
    internal sealed class TopicKeyComparer : IEqualityComparer<ReadOnlyMemory<byte>>
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

    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, Action<ReadOnlyMemory<byte>>> _handlers =
        new(new TopicKeyComparer());

    /// <summary>
    /// Registers or replaces the handler for the specified topic.
    /// </summary>
    /// <param name="topic">The topic to register the handler for.</param>
    /// <param name="handler">The action to invoke when an event notification is received for this topic.</param>
    public void Set(ReadOnlyMemory<byte> topic, Action<ReadOnlyMemory<byte>> handler)
    {
        _handlers.AddOrUpdate(topic,
            _ => handler,
            (_, _) => handler);
    }

    /// <summary>
    /// Removes the handler for the specified topic.
    /// </summary>
    /// <param name="topic">The topic to remove the handler for.</param>
    /// <returns><c>true</c> if the handler was found and removed; <c>false</c> otherwise.</returns>
    public bool Remove(ReadOnlyMemory<byte> topic)
    {
        return _handlers.TryRemove(topic, out _);
    }

    /// <summary>
    /// Invokes the handler for the specified topic with the given message payload.
    /// </summary>
    /// <param name="topic">The topic whose handler to invoke.</param>
    /// <param name="message">The message payload to pass to the handler.</param>
    public void Invoke(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> message)
    {
        if (_handlers.TryGetValue(topic, out var handler))
            handler(message);
    }
}
