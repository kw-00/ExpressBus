using System.Collections.Concurrent;
using System.Buffers.Binary;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

namespace ExpressBus.Client;

/// <summary>
/// Concrete implementation of <see cref="IClientMessenger"/> over a TCP connection.
/// </summary>
/// <remarks>
/// Wraps an <see cref="IConnection"/> to send typed requests and receive typed responses.
/// Uses a background listener thread to receive responses and correlate them with pending
/// requests via <see cref="IRequestAssociated.RequestId"/>. The wire format is:
/// <list type="bullet">
///   <item><description>Request: <c>[1-byte type][N-byte payload]</c> (generated <c>ToBytes</c> handles serialization).</description></item>
///   <item><description>Response: <c>[1-byte type][4-byte size LE int32][N-byte payload]</c>.</description></item>
/// </list>
/// </remarks>
public sealed class ClientMessenger : IClientMessenger, IAsyncDisposable
{
    private readonly IConnectionFactory _factory;
    private readonly Address _address;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ReadOnlyMemory<byte>>> _pendingRequests = new();
    private volatile bool _disposed;

    private IConnection? _connection;
    private CancellationTokenSource? _listenerCts;
    private bool _started;

    /// <summary>
    /// Invoked when an <see cref="EventNotification"/> arrives from the server.
    /// </summary>
    public Func<EventNotification, Task>? Event { get; set; }

    /// <summary>
    /// Creates a <see cref="ClientMessenger"/> configured to connect to the specified address.
    /// Call <see cref="StartAsync"/> to establish the connection.
    /// </summary>
    /// <param name="factory">The factory used to create the underlying connection.</param>
    /// <param name="address">The remote address to connect to.</param>
    public ClientMessenger(IConnectionFactory factory, Address address)
    {
        _factory = factory;
        _address = address;
    }

    private IConnection Connection => _connection ?? throw new InvalidOperationException("ClientMessenger has not been started. Call StartAsync().");

    /// <inheritdoc />
    public Task StartAsync()
    {
        if (_started)
            return Task.CompletedTask;

        _started = true;

        _connection = _factory.CreateConnection(_address);
        _connection!.Closed += OnConnectionClosed;
        _listenerCts = new CancellationTokenSource();
        Task.Factory.StartNew(() => ResponseListenerAsync(_listenerCts.Token).GetAwaiter().GetResult(), TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<BroadcastResponse> SendBroadcastRequestAsync(BroadcastRequest request)
        => await SendRequestResponseAsync<BroadcastRequest, BroadcastResponse>(
            id => new BroadcastRequest(id, request.Topic, request.Message)).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<SubscribeResponse> SendSubscribeRequestAsync(SubscribeRequest request)
        => await SendRequestResponseAsync<SubscribeRequest, SubscribeResponse>(
            id => new SubscribeRequest(id, request.Topic)).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<UnsubscribeResponse> SendUnsubscribeRequestAsync(UnsubscribeRequest request)
        => await SendRequestResponseAsync<UnsubscribeRequest, UnsubscribeResponse>(
            id => new UnsubscribeRequest(id, request.Topic)).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<UnsubscribeAllResponse> SendUnsubscribeAllRequestAsync(UnsubscribeAllRequest request)
        => await SendRequestResponseAsync<UnsubscribeAllRequest, UnsubscribeAllResponse>(
            id => new UnsubscribeAllRequest(id)).ConfigureAwait(false);

    private async Task<TResponse> SendRequestResponseAsync<TRequest, TResponse>(
        Func<Guid, TRequest> createRequest)
        where TRequest : struct, IByteSerializable<TRequest>, IMessageSize, IRequestAssociated
        where TResponse : struct, IByteSerializable<TResponse>
    {
        var request = createRequest(Guid.NewGuid());
        var raw = await SendRequestAsync(request).ConfigureAwait(false);
        return TResponse.FromBytes(raw.ToArray());
    }

    private async Task<ReadOnlyMemory<byte>> SendRequestAsync<TRequest>(TRequest request)
        where TRequest : struct, IByteSerializable<TRequest>, IMessageSize, IRequestAssociated
    {
        var requestId = request.RequestId;

        var tcs = new TaskCompletionSource<ReadOnlyMemory<byte>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests.TryAdd(requestId, tcs);

        try
        {
            // Serialize request: ToBytes writes [type byte][field bytes]
            var payloadSize = request.ByteSize;
            using var payload = new DisposableMemory(payloadSize);
            request.ToBytes(payload.Memory);
            await Connection.SendAsync(payload.Memory).ConfigureAwait(false);

            // Wait for background listener to complete the TCS with the raw response bytes
            return await tcs.Task.ConfigureAwait(false);
        }
        catch
        {
            _pendingRequests.TryRemove(requestId, out _);
            tcs.TrySetException(new IOException("Request failed"));
            throw;
        }
    }

    private async Task ResponseListenerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Read 1-byte MessageTypeIdentifier
                using var typeBuffer = new DisposableMemory(1);
                await Connection.ReceiveFullAsync(typeBuffer.Memory).ConfigureAwait(false);
                var responseType = typeBuffer.Memory.Span[0];

                // Read 4-byte size (LE int32)
                using var sizeBuffer = new DisposableMemory(4);
                await Connection.ReceiveFullAsync(sizeBuffer.Memory).ConfigureAwait(false);
                var responseSize = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer.Memory.Span);

                // Read payload
                using var payloadBuffer = new DisposableMemory(responseSize);
                await Connection.ReceiveFullAsync(payloadBuffer.Memory).ConfigureAwait(false);
                var payload = payloadBuffer.Memory;

                // Deserialize to extract RequestId, then complete the matching TCS
                TaskCompletionSource<ReadOnlyMemory<byte>>? tcs = null;
                if (responseType == BroadcastResponse.MessageTypeIdentifier)
                {
                    tcs = TryCompleteRequest<BroadcastResponse>(payload, payload);
                }
                else if (responseType == SubscribeResponse.MessageTypeIdentifier)
                {
                    tcs = TryCompleteRequest<SubscribeResponse>(payload, payload);
                }
                else if (responseType == UnsubscribeAllResponse.MessageTypeIdentifier)
                {
                    tcs = TryCompleteRequest<UnsubscribeAllResponse>(payload, payload);
                }
                else if (responseType == UnsubscribeResponse.MessageTypeIdentifier)
                {
                    tcs = TryCompleteRequest<UnsubscribeResponse>(payload, payload);
                }
                else if (responseType == EventNotification.MessageTypeIdentifier)
                {
                    var msg = EventNotification.FromBytes(payload);
                    await (Event?.Invoke(msg) ?? Task.CompletedTask).ConfigureAwait(false);
                }
                else
                {
                    // Unknown response type — skip (protocol error, don't crash)
                    continue;
                }

                tcs?.TrySetResult(payload);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (IOException)
        {
            // Connection closed — pending TCSs will be handled by OnConnectionClosed
        }
        catch
        {
            // Unexpected error — let OnConnectionClosed handle cleanup
        }
    }

    private TaskCompletionSource<ReadOnlyMemory<byte>>? TryCompleteRequest<TResponse>(
        Memory<byte> buffer, ReadOnlyMemory<byte> payload)
        where TResponse : struct, IByteSerializable<TResponse>, IRequestAssociated
    {
        var msg = TResponse.FromBytes(buffer);
        return _pendingRequests.TryRemove(msg.RequestId, out var found) ? found : null;
    }

    private void OnConnectionClosed(CloseMode mode)
    {
        _listenerCts?.Cancel();
        foreach (var tcs in _pendingRequests.Values)
            tcs.TrySetException(new IOException("Connection closed"));
        _pendingRequests.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _listenerCts?.Cancel();
        _listenerCts?.Dispose();

        foreach (var tcs in _pendingRequests.Values)
            tcs.TrySetCanceled();
        _pendingRequests.Clear();

        var connection = _connection;
        if (connection != null)
        {
            connection.Closed -= OnConnectionClosed;
            await connection.CloseAsync(CloseMode.Shutdown).ConfigureAwait(false);
        }
    }
}
