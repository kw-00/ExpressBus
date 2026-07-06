using ExpressBus.Protocol.Bus;

namespace ExpressBus.Protocol;

/// <summary>
/// Server-side interface for sending fire-and-forget notifications to clients.
/// </summary>
/// <remarks>
/// Notifications reverse the typical request/response role: the server pushes
/// messages to subscribed clients without waiting for a response. A concrete
/// implementation serializes the notification and writes it to connected client streams.
/// </remarks>
public interface INotificationSender
{
    /// <summary>
    /// Sends an event notification to all subscribers of a topic.
    /// </summary>
    Task SendEventNotificationAsync(EventNotification notification);
}
