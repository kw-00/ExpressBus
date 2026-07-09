namespace ExpressBus.Client;

/// <summary>
/// Message broadcasting for the ExpressBus pub/sub protocol.
/// </summary>
public interface IBroadcaster
{
    /// <summary>
    /// Broadcasts a message to all subscribers of the specified topic.
    /// </summary>
    /// <param name="topic">The topic to broadcast to.</param>
    /// <param name="message">The message payload to broadcast.</param>
    Task BroadcastAsync(ReadOnlyMemory<byte> topic, ReadOnlyMemory<byte> message);
}
