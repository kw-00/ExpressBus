using System.Buffers.Binary;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;
using ExpressBus.Transfer.Tcp;

namespace ExpressBus.Provider;

/// <summary>
/// TCP-based message broker that accepts connections and dispatches requests.
/// </summary>
/// <remarks>
/// Each accepted connection runs a loop that reads requests, dispatches them via
/// internal helper methods, and sends responses back over the same connection.
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
    protected override async Task HandleConnectionAsync(IConnection connection, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connection.Closed += mode => cts.Cancel();

        try
        {
            var handler = new ConnectionHandling(connection, _topicTracker, _logger);
            await handler.HandleConnectionRequestsAsync(cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing connection");
        }
        finally
        {
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
                _topicTracker.RemoveSubscriber(connection);
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
