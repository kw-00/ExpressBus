using System.Net;
using System.Net.Sockets;
using ExpressBus.Transfer.Tcp;

namespace ExpressBus.Transfer;

/// <summary>
/// Abstract server that accepts incoming connections and wraps them as <see cref="IConnection"/>.
/// Leaves <see cref="HandleConnectionAsync"/> unimplemented — concrete subclasses handle per-connection logic.
/// </summary>
public abstract class ServerBase : IServer
{
	private readonly Address _address;
	private readonly IConnectionFactory _connectionFactory;
	private readonly CancellationTokenSource _cts = new();
	private readonly SemaphoreSlim _lock = new(1, 1);
	private int _disposed;

	private HashSet<Socket> _clientSockets = new();
	private Socket? _listeningSocket;

	/// <summary>
	/// Creates a new <see cref="ServerBase"/> bound to the given <paramref name="address"/>.
	/// </summary>
	/// <param name="address">The address to bind and listen on.</param>
	/// <param name="connectionFactory">Factory for creating and configuring sockets.</param>
	public ServerBase(Address address, IConnectionFactory connectionFactory)
	{
		_address = address ?? throw new ArgumentNullException(nameof(address));
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
	}

	/// <inheritdoc />
	public async Task ListenAsync()
	{
		// Pre-lock: refuse if already disposed or cancellation already requested
		if (System.Threading.Volatile.Read(ref _disposed) != 0 || _cts.Token.IsCancellationRequested)
			throw new ObjectDisposedException(GetType().Name);

		// Try to acquire the lock; fail immediately if another ListenAsync is running or DisposeAsync is in progress
		if (!await _lock.WaitAsync(0).ConfigureAwait(false))
			throw new InvalidOperationException(
				"Cannot start server, as it is already running or in the process of stopping.");

		try
		{
			// Post-lock: check again in case DisposeAsync cancelled the token before we acquired the lock
			if (_cts.Token.IsCancellationRequested)
				throw new ObjectDisposedException(GetType().Name);

			_listeningSocket = _connectionFactory.CreateListeningSocket();
			_listeningSocket.Bind(new IPEndPoint(SocketEndpoints.Resolve(_address.Host), _address.Port));
			_listeningSocket.Listen();

			// Post-setup: check one more time after socket creation
			if (_cts.Token.IsCancellationRequested)
				throw new ObjectDisposedException(GetType().Name);

			while (!_cts.Token.IsCancellationRequested)
			{
				Socket? client = null;
				try
				{
					client = await _listeningSocket.AcceptAsync(_cts.Token).ConfigureAwait(false);
					_clientSockets.Add(client);
					_ = HandleConnectionAsync(_connectionFactory.CreateConnectionFromAcceptedSocket(client), _cts.Token);
				}
				catch (Exception ex) when (_cts.Token.IsCancellationRequested &&
					(ex is ObjectDisposedException || ex is SocketException))
				{
					break;
				}
				finally
				{
					if (client is not null)
						_clientSockets.Remove(client);
				}
			}
		}
		finally
		{
			CloseListeningSocket();
			_lock.Release();
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		// Cancel before acquiring the lock so ListenAsync can see the cancellation and back out
		_cts.Cancel();

		// Wait indefinitely for ListenAsync to finish and release the lock
		await _lock.WaitAsync().ConfigureAwait(false);

		try
		{
			// Mark as disposed within the lock
			Interlocked.Exchange(ref _disposed, 1);

			// Shut down and close all client sockets
			foreach (var socket in _clientSockets)
			{
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}
			_clientSockets.Clear();

			await CleanupOnCloseAsync().ConfigureAwait(false);

			CloseListeningSocket();
		}
		finally
		{
			_cts.Dispose();
			_lock.Release();
			_lock.Dispose();
		}
	}

	protected abstract Task HandleConnectionAsync(IConnection connection, CancellationToken cancellationToken);

	protected virtual Task CleanupOnCloseAsync() => Task.CompletedTask;

	private void CloseListeningSocket()
	{
		var socket = _listeningSocket;
		if (socket is not null)
		{
			_listeningSocket = null;
			socket.Close();
		}
	}
}
