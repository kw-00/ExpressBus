namespace ExpressBus.Client;

/// <summary>
/// Subscription management for the ExpressBus pub/sub protocol.
/// </summary>
public interface ISubscriber
{
    /// <summary>
    /// Registers an async handler for the specified topic.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handler">The async action invoked when a notification arrives for this topic.</param>
    Task SubscribeAsync(ReadOnlyMemory<byte> topic, Func<ReadOnlyMemory<byte>, Task> handler);

    /// <summary>
    /// Removes all handlers for the specified topic.
    /// </summary>
    /// <param name="topic">The topic to unsubscribe from.</param>
    Task UnsubscribeAsync(ReadOnlyMemory<byte> topic);
}
