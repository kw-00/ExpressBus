using ExpressBus.Transfer;
using ExpressBus.Transfer.Tcp;

namespace ExpressBus.Provider;

/// <summary>
/// TCP-based message broker that accepts connections and dispatches requests.
/// </summary>
/// <remarks>
/// Each accepted connection runs a loop that reads requests, dispatches them via
/// <see cref="RequestHandler"/>, and sends responses back over the same connection.
/// The loop exits when the connection closes — signalled via <see cref="IConnection.Closed"/>,
/// which triggers a <see cref="CancellationTokenSource"/> that cancels the loop.
/// On connection exit, the client is removed from all tracked topics.
/// </remarks>
public sealed class BrokerServer : ServerBase
{
    private readonly TopicTracker _topicTracker;
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a new <see cref="BrokerServer"/>.
    /// </summary>
    /// <param name="address">The address to bind and listen on.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public BrokerServer(Address address, ILogger? logger = null)
        : base(address, new TcpConnectionFactory())
    {
        _topicTracker = new TopicTracker();
        _logger = logger;
    }


    /// <inheritdoc />
    protected override async Task HandleConnectionAsync(IConnection connection)
    {
        var handler = new RequestHandler(connection, _topicTracker, _logger);
        var cts = new CancellationTokenSource();

        // Wire connection close to cancellation
        connection.Closed += mode => cts.Cancel();

        try
        {
            while (!cts.IsCancellationRequested)
            {
                await handler.HandleRequestAsync(cts.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing connection");
        }
        finally
        {
            // Close the connection gracefully
            try
            {
                await connection.CloseAsync(CloseMode.Shutdown).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to close connection gracefully");
            }
            finally
            {
                // Clean up all topic subscriptions for this connection
                _topicTracker.RemoveSubscriber(connection);
                cts.Dispose();
            }

        }
    }

    /// <inheritdoc />
    protected override Task CleanupOnCloseAsync()
    {
        _topicTracker.ClearAll();
        return Task.CompletedTask;
    }
}
