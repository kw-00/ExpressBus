using System.Collections.Concurrent;
using System.Buffers.Binary;
using ExpressBus.Buffering;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

namespace ExpressBus.Client;

/// <summary>
/// Concrete implementation of <see cref="IRequestSender"/> over a TCP connection.
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
public sealed class RequestSender : IRequestSender, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ReadOnlyMemory<byte>>> _pendingRequests = new();
    private readonly CancellationTokenSource _listenerCts = new();
    private volatile bool _disposed;

    /// <summary>
    /// Creates a <see cref="RequestSender"/> over the specified connection.
    /// </summary>
    /// <param name="connection">The connection to send requests over.</param>
    public RequestSender(IConnection connection)
    {
        _connection = connection;
        connection.Closed += OnConnectionClosed;
        Task.Factory.StartNew(() => ResponseListenerAsync(_listenerCts.Token).GetAwaiter().GetResult(), TaskCreationOptions.LongRunning);
    }

    /// <inheritdoc />
    public async Task<BroadcastResponse> SendBroadcastRequestAsync(BroadcastRequest request)
    {
        var requestId = Guid.NewGuid();
        var raw = await SendRequestAsync(new BroadcastRequest(requestId, request.Topic, request.Message))
            .ConfigureAwait(false);
        return BroadcastResponse.FromBytes(raw.ToArray());
    }

    /// <inheritdoc />
    public async Task<SubscribeResponse> SendSubscribeRequestAsync(SubscribeRequest request)
    {
        var requestId = Guid.NewGuid();
        var raw = await SendRequestAsync(new SubscribeRequest(requestId, request.Topic))
            .ConfigureAwait(false);
        return SubscribeResponse.FromBytes(raw.ToArray());
    }

    /// <inheritdoc />
    public async Task<UnsubscribeResponse> SendUnsubscribeRequestAsync(UnsubscribeRequest request)
    {
        var requestId = Guid.NewGuid();
        var raw = await SendRequestAsync(new UnsubscribeRequest(requestId, request.Topic))
            .ConfigureAwait(false);
        return UnsubscribeResponse.FromBytes(raw.ToArray());
    }

    /// <inheritdoc />
    public async Task<UnsubscribeAllResponse> SendUnsubscribeAllRequestAsync(UnsubscribeAllRequest request)
    {
        var requestId = Guid.NewGuid();
        var raw = await SendRequestAsync(new UnsubscribeAllRequest(requestId))
            .ConfigureAwait(false);
        return UnsubscribeAllResponse.FromBytes(raw.ToArray());
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
            await _connection.SendAsync(payload.Memory).ConfigureAwait(false);

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
                await _connection.ReceiveFullAsync(typeBuffer.Memory).ConfigureAwait(false);
                var responseType = typeBuffer.Memory.Span[0];

                // Read 4-byte size (LE int32)
                using var sizeBuffer = new DisposableMemory(4);
                await _connection.ReceiveFullAsync(sizeBuffer.Memory).ConfigureAwait(false);
                var responseSize = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer.Memory.Span);

                // Read payload
                using var payloadBuffer = new DisposableMemory(responseSize);
                await _connection.ReceiveFullAsync(payloadBuffer.Memory).ConfigureAwait(false);
                var payload = payloadBuffer.Memory;

                // Deserialize to extract RequestId, then complete the matching TCS
                TaskCompletionSource<ReadOnlyMemory<byte>>? tcs = null;
                if (responseType == BroadcastResponse.MessageTypeIdentifier)
                {
                    var msg = BroadcastResponse.FromBytes(payload);
                    tcs = _pendingRequests.TryRemove(msg.RequestId, out var found) ? found : null;
                }
                else if (responseType == SubscribeResponse.MessageTypeIdentifier)
                {
                    var msg = SubscribeResponse.FromBytes(payload);
                    tcs = _pendingRequests.TryRemove(msg.RequestId, out var found) ? found : null;
                }
                else if (responseType == UnsubscribeAllResponse.MessageTypeIdentifier)
                {
                    var msg = UnsubscribeAllResponse.FromBytes(payload);
                    tcs = _pendingRequests.TryRemove(msg.RequestId, out var found) ? found : null;
                }
                else if (responseType == UnsubscribeResponse.MessageTypeIdentifier)
                {
                    var msg = UnsubscribeResponse.FromBytes(payload);
                    tcs = _pendingRequests.TryRemove(msg.RequestId, out var found) ? found : null;
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

    private void OnConnectionClosed(CloseMode mode)
    {
        _listenerCts.Cancel();
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

        _listenerCts.Cancel();
        _listenerCts.Dispose();

        foreach (var tcs in _pendingRequests.Values)
            tcs.TrySetCanceled();
        _pendingRequests.Clear();

        _connection.Closed -= OnConnectionClosed;

        await _connection.CloseAsync(CloseMode.Shutdown).ConfigureAwait(false);
    }
}
