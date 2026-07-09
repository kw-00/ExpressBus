using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;

namespace ExpressBus.Client;

/// <summary>
/// Aggregates multiple <see cref="ClientMessenger"/> instances pooled from a <see cref="ClientMessengerPool"/>.
/// </summary>
/// <remarks>
/// Each call to a request method acquires a messenger from the pool, invokes the corresponding
/// operation, and returns the messenger. The <see cref="Event"/> callback is aggregated across
/// all messengers — it fires whenever any pooled messenger receives an event notification.
/// </remarks>
public sealed class EnsembleClientMessenger : IClientMessenger, IAsyncDisposable
{
    private readonly ClientMessengerPool _pool;
    private readonly CancellationTokenSource _disposedCts = new();
    private volatile bool _disposed;

    /// <summary>
    /// Creates an <see cref="EnsembleClientMessenger"/> backed by the specified pool.
    /// </summary>
    /// <param name="pool">The pool of messengers to draw from.</param>
    public EnsembleClientMessenger(ClientMessengerPool pool)
    {
        _pool = pool;
    }

    /// <inheritdoc />
    public Func<EventNotification, Task>? Event { get; set; }

    /// <inheritdoc />
    public async Task<BroadcastResponse> SendBroadcastRequestAsync(BroadcastRequest request)
    {
        var messenger = await _pool.GetAsync().ConfigureAwait(false);
        try
        {
            return await messenger.SendBroadcastRequestAsync(request).ConfigureAwait(false);
        }
        finally
        {
            _pool.Return(messenger);
        }
    }

    /// <inheritdoc />
    public async Task<SubscribeResponse> SendSubscribeRequestAsync(SubscribeRequest request)
    {
        var messenger = await _pool.GetAsync().ConfigureAwait(false);
        try
        {
            return await messenger.SendSubscribeRequestAsync(request).ConfigureAwait(false);
        }
        finally
        {
            _pool.Return(messenger);
        }
    }

    /// <inheritdoc />
    public async Task<UnsubscribeResponse> SendUnsubscribeRequestAsync(UnsubscribeRequest request)
    {
        var messenger = await _pool.GetAsync().ConfigureAwait(false);
        try
        {
            return await messenger.SendUnsubscribeRequestAsync(request).ConfigureAwait(false);
        }
        finally
        {
            _pool.Return(messenger);
        }
    }

    /// <inheritdoc />
    public async Task<UnsubscribeAllResponse> SendUnsubscribeAllRequestAsync(UnsubscribeAllRequest request)
    {
        var messenger = await _pool.GetAsync().ConfigureAwait(false);
        try
        {
            return await messenger.SendUnsubscribeAllRequestAsync(request).ConfigureAwait(false);
        }
        finally
        {
            _pool.Return(messenger);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _disposedCts.Dispose();
        await _pool.DisposeAsync().ConfigureAwait(false);
    }
}
