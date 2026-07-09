using System.Collections.Concurrent;
using ExpressBus.Protocol;
using ExpressBus.Transfer;

namespace ExpressBus.Client;

/// <summary>
/// Fixed-size pool of <see cref="ClientMessenger"/> instances backed by a <see cref="ConcurrentBag{T}"/>.
/// </summary>
/// <remarks>
/// Creates <paramref name="size"/> messengers on construction, each connected to the same
/// <paramref name="address"/> via the provided <paramref name="factory"/>. Callers acquire
/// a messenger via <see cref="GetAsync"/>, use it, and return it via <see cref="Return"/>.
/// The pool is disposable — disposing it closes all connections and releases resources.
/// </remarks>
public sealed class ClientMessengerPool : IAsyncDisposable
{
    private readonly ConcurrentBag<ClientMessenger> _pool;
    private readonly int _size;
    private readonly SemaphoreSlim _semaphore;
    private volatile bool _disposed;

    /// <summary>
    /// Creates a pool with the specified number of <see cref="ClientMessenger"/> instances.
    /// </summary>
    /// <param name="factory">The factory used to create connections for each messenger.</param>
    /// <param name="address">The remote address each messenger will connect to.</param>
    /// <param name="size">The number of messengers to pre-create in the pool.</param>
    public ClientMessengerPool(IConnectionFactory factory, Address address, int size)
    {
        _size = size;
        _semaphore = new SemaphoreSlim(size, size);
        _pool = new ConcurrentBag<ClientMessenger>();

        for (var i = 0; i < size; i++)
        {
            _pool.Add(new ClientMessenger(factory, address));
        }
    }

    /// <summary>
    /// Acquires a <see cref="ClientMessenger"/> from the pool, waiting if all are in use.
    /// </summary>
    /// <returns>A <see cref="ClientMessenger"/> ready to send requests.</returns>
    public async Task<ClientMessenger> GetAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);

        ClientMessenger? messenger = null;
        while (!_pool.TryTake(out messenger))
        {
            // Should not happen if semaphore and bag are in sync, but wait briefly and retry
            await Task.Delay(10).ConfigureAwait(false);
        }

        return messenger!;
    }

    /// <summary>
    /// Returns a <see cref="ClientMessenger"/> to the pool.
    /// </summary>
    /// <param name="messenger">The messenger to return.</param>
    public void Return(ClientMessenger messenger)
    {
        _pool.Add(messenger);
        _semaphore.Release();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        var tasks = new List<Task>();
        while (_pool.TryTake(out var messenger))
        {
            tasks.Add(messenger.DisposeAsync().AsTask());
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        _semaphore.Dispose();
    }
}
