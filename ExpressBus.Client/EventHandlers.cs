using System.Collections.Generic;
using ExpressBus.Concurrency;
using ExpressBus.DataStructures;

namespace ExpressBus.Client;

/// <summary>
/// Thread-safe registry of topic-to-handler delegates for incoming event notifications.
/// </summary>
/// <remarks>
/// Topics are keyed by <see cref="ReadOnlyMemory{T}"/> contents and each topic maps to a
/// collection of <see cref="Action{T}"/> handlers that receive the notification payload.
/// Uses partitioned locking to reduce contention.
/// </remarks>
public sealed class EventHandlers
{
    private readonly Grouping<ReadOnlyMemory<byte>, Action<ReadOnlyMemory<byte>>> _handlers;

    /// <summary>
    /// Creates a new <see cref="EventHandlers"/> instance.
    /// </summary>
    public EventHandlers()
    {
        _handlers = new Grouping<ReadOnlyMemory<byte>, Action<ReadOnlyMemory<byte>>>(
            TopicKeyComparer.Instance,
            HashProducers.ForReadOnlyMemoryByte);
    }

    /// <summary>
    /// Registers a handler for the specified topic. Multiple handlers can be registered
    /// for the same topic.
    /// </summary>
    /// <param name="topic">The topic to register the handler for.</param>
    /// <param name="handler">The action to invoke when an event notification is received for this topic.</param>
    public void Set(ReadOnlyMemory<byte> topic, Action<ReadOnlyMemory<byte>> handler)
    {
        _handlers.Add(topic, handler);
    }

    /// <summary>
    /// Removes a specific handler from the specified topic.
    /// </summary>
    /// <param name="topic">The topic to remove the handler from.</param>
    /// <param name="handler">The handler to remove.</param>
    /// <returns><c>true</c> if the handler was found and removed; <c>false</c> otherwise.</returns>
    public bool Remove(ReadOnlyMemory<byte> topic, Action<ReadOnlyMemory<byte>> handler)
    {
        return _handlers.Remove(topic, handler);
    }

    /// <summary>
    /// Removes all handlers for the specified topic.
    /// </summary>
    /// <param name="topic">The topic to remove all handlers from.</param>
    public void Remove(ReadOnlyMemory<byte> topic)
    {
        var handlers = GetHandlers(topic);
        foreach (var handler in handlers)
        {
            _handlers.Remove(topic, handler);
        }
    }

    /// <summary>
    /// Returns a copy of the handler set for the specified topic.
    /// </summary>
    /// <param name="topic">The topic to look up.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing copies of all handlers for the topic,
    /// or an empty set if no handlers are registered.</returns>
    internal HashSet<Action<ReadOnlyMemory<byte>>> GetHandlers(ReadOnlyMemory<byte> topic)
    {
        return _handlers.Get(topic);
    }

    /// <summary>
    /// Invokes all handlers for the specified topic with the given message payload.
    /// </summary>
    /// <param name="topic">The topic whose handlers to invoke.</param>
    /// <param name="message">The message payload to pass to each handler.</param>
    public void Invoke(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> message)
    {
        var handlers = _handlers.Get(topic);
        foreach (var handler in handlers)
        {
            handler(message);
        }
    }
}
