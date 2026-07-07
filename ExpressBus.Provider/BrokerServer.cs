using ExpressBus.Transfer;
using ExpressBus.Transfer.Tcp;

namespace ExpressBus.Provider;

/// <summary>
/// TCP-based message broker server that accepts connections and dispatches requests.
/// </summary>
/// <remarks>
/// Wraps a <see cref="TopicTracker"/> to form a complete TCP message broker.
/// Each accepted connection receives its own <see cref="RequestHandler"/> instance
/// (created via the factory delegate in <c>HandleConnectionAsync</c>) which runs the
/// full request-response loop: read a request, dispatch to the handler (which
/// serializes and sends the response), and loop. On any connection exit (error,
/// close, exception), the connection is removed from all tracked topics.
/// </remarks>
public sealed class BrokerServer : TcpServer
{
    private readonly TopicTracker _topicTracker;
    private readonly Func<IConnection, RequestHandler> _handlerFactory;
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a new <see cref="BrokerServer"/>.
    /// </summary>
    /// <param name="address">The address to bind and listen on.</param>
    /// <param name="topicTracker">The shared topic tracker for managing subscriptions.</param>
    /// <param name="handlerFactory">
    /// A factory that creates a <see cref="RequestHandler"/> for each new connection.
    /// </param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public BrokerServer(
        Address address,
        TopicTracker topicTracker,
        Func<IConnection, RequestHandler> handlerFactory,
        ILogger? logger = null)
        : base(address)
    {
        _topicTracker = topicTracker;
        _handlerFactory = handlerFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task HandleConnectionAsync(IConnection connection)
    {
        var handler = _handlerFactory(connection);
        try
        {
            await handler.HandleRequestAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing connection");
        }
        finally
        {
            _topicTracker.RemoveSubscriber(connection);
        }
    }
}
