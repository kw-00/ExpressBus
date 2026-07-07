using System.Buffers;
using System.Buffers.Binary;
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;
using ExpressBus.Transfer;

namespace ExpressBus.Client;

/// <summary>
/// Concrete implementation of <see cref="IRequestSender"/> over a TCP connection.
/// </summary>
/// <remarks>
/// Wraps an <see cref="IConnection"/> to send typed requests and receive typed responses.
/// The wire format is:
/// <list type="bullet">
///   <item><description>Request: <c>[1-byte type][N-byte payload]</c> (generated <c>ToBytes</c> handles serialization).</description></item>
///   <item><description>Response: <c>[1-byte type][4-byte size LE int32][N-byte payload]</c>.</description></item>
/// </list>
/// </remarks>
public sealed class RequestSender : IRequestSender
{
    private readonly IConnection _connection;

    /// <summary>
    /// Creates a <see cref="RequestSender"/> over the specified connection.
    /// </summary>
    /// <param name="connection">The connection to send requests over.</param>
    public RequestSender(IConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async Task<BroadcastResponse> SendBroadcastRequestAsync(BroadcastRequest request)
        => await SendRequestAsync(request, BroadcastResponse.FromBytes).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<SubscribeResponse> SendSubscribeRequestAsync(SubscribeRequest request)
        => await SendRequestAsync(request, SubscribeResponse.FromBytes).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<UnsubscribeResponse> SendUnsubscribeRequestAsync(UnsubscribeRequest request)
        => await SendRequestAsync(request, UnsubscribeResponse.FromBytes).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<UnsubscribeAllResponse> SendUnsubscribeAllRequestAsync(UnsubscribeAllRequest request)
        => await SendRequestAsync(request, UnsubscribeAllResponse.FromBytes).ConfigureAwait(false);

    private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        Func<Memory<byte>, TResponse> deserializer)
        where TRequest : struct, IByteSerializable<TRequest>, IMessageSize
        where TResponse : struct, IByteSerializable<TResponse>, IMessageSize
    {
        // Serialize request: ToBytes writes [type byte][field bytes]
        var payloadSize = request.ByteSize;
        var rentBuffer = MemoryPool<byte>.Shared.Rent(payloadSize);
        try
        {
            request.ToBytes(rentBuffer.Memory.Slice(0, payloadSize));
            await _connection.SendAsync(rentBuffer.Memory.Slice(0, payloadSize)).ConfigureAwait(false);

            // Read response: [1-byte type][4-byte size][payload]
            var typeBuffer = MemoryPool<byte>.Shared.Rent(1);
            await _connection.ReceiveFullAsync(typeBuffer.Memory).ConfigureAwait(false);
            typeBuffer.Dispose();

            var sizeBuffer = MemoryPool<byte>.Shared.Rent(4);
            await _connection.ReceiveFullAsync(sizeBuffer.Memory).ConfigureAwait(false);
            var responseSize = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer.Memory.Span);
            sizeBuffer.Dispose();

            var responseRent = MemoryPool<byte>.Shared.Rent(responseSize);
            await _connection.ReceiveFullAsync(responseRent.Memory.Slice(0, responseSize)).ConfigureAwait(false);
            var response = deserializer(responseRent.Memory.Slice(0, responseSize));
            responseRent.Dispose();

            return response;
        }
        finally
        {
            rentBuffer.Dispose();
        }
    }
}
