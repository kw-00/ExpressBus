using System.Buffers;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

namespace ExpressBus.Client;

/// <summary>
/// Client-side notification handler that dispatches incoming event notifications
/// to registered handlers in an <see cref="EventHandlers"/> registry.
/// </summary>
public sealed class ClientNotificationHandler : NotificationHandlerBase
{
    private readonly EventHandlers _eventHandlers;

    /// <summary>
    /// Creates a new <see cref="ClientNotificationHandler"/>.
    /// </summary>
    /// <param name="connection">The connection to read notifications from.</param>
    /// <param name="eventHandlers">The event handler registry to dispatch notifications to.</param>
    public ClientNotificationHandler(IConnection connection, EventHandlers eventHandlers)
        : base(connection)
    {
        _eventHandlers = eventHandlers;
    }

    /// <inheritdoc />
    protected override IMemoryOwner<byte> CreateBuffer(int size) =>
        MemoryPool<byte>.Shared.Rent(size);

    /// <inheritdoc />
    protected override Task HandleEventNotificationAsync(EventNotification notification)
    {
        _eventHandlers.Invoke(notification.Topic.Data, notification.Message.Data);
        return Task.CompletedTask;
    }
}
