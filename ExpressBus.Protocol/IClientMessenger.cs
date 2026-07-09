using ExpressBus.Protocol.Bus;

namespace ExpressBus.Protocol;

/// <summary>
/// Client-side protocol interface for the ExpressBus messaging system.
/// </summary>
/// <remarks>
/// <para>
/// This is the client's endpoint for the protocol contract. A concrete implementation sends
/// typed requests over the wire and deserializes the results. Each method corresponds to a
/// request type the server understands and returns the matching result type.
/// </para>
/// <para>
/// <see cref="RequestHandlerBase"/> is the server-side counterpart — it accepts a stream,
/// reads the message header (type byte + 4-byte size), deserializes the payload, dispatches
/// to typed handlers, and returns serialized results. Together the two types enforce a single,
/// bidirectional protocol: every operation defined on the client has a matching handler on the
/// server, and both sides use the same request/result types and wire format.
/// </para>
/// </remarks>
public interface IClientMessenger
{
    /// <summary>
    /// Invoked when an <see cref="EventNotification"/> arrives from the server.
    /// </summary>
    Func<EventNotification, Task>? Event { get; set; }

    /// <summary>
    /// Establishes the connection to the server and starts the response listener.
    /// Must be called before any send methods.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Sends a broadcast request to the server and returns the response.
    /// </summary>
    Task<BroadcastResponse> SendBroadcastRequestAsync(BroadcastRequest request);

    /// <summary>
    /// Sends a subscribe request to the server and returns the response.
    /// </summary>
    Task<SubscribeResponse> SendSubscribeRequestAsync(SubscribeRequest request);

    /// <summary>
    /// Sends an unsubscribe request to the server and returns the response.
    /// </summary>
    Task<UnsubscribeResponse> SendUnsubscribeRequestAsync(UnsubscribeRequest request);

    /// <summary>
    /// Sends an unsubscribe-all request to the server and returns the response.
    /// </summary>
    Task<UnsubscribeAllResponse> SendUnsubscribeAllRequestAsync(UnsubscribeAllRequest request);
}
