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
	private HashSet<Socket> _clientSockets = new();
	private Socket? _listeningSocket;

	private CancellationTokenSource? _stoppingTokenSource;
	private readonly SemaphoreSlim _stoppingTokenLock = new SemaphoreSlim(1, 1);

	private readonly SemaphoreSlim _serverLock = new SemaphoreSlim(1, 1);

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
	public async void StopAsync()
	{
		try
		{
			await _stoppingTokenLock.WaitAsync();

			_stoppingTokenSource?.Cancel();
			_stoppingTokenSource?.Dispose();
			Interlocked.Exchange<CancellationTokenSource?>(ref _stoppingTokenSource, null);
			try
			{
				await _serverLock.WaitAsync();
				foreach (var socket in _clientSockets)
				{
					socket.Shutdown(SocketShutdown.Both);
					socket.Close();
				}
				await CleanupOnCloseAsync().ConfigureAwait(false);
				CloseListeningSocket();
			}
			finally
			{
				_serverLock.Release();
			}
		}
		finally
		{
			_stoppingTokenLock.Release();
		}
	}

	/// <inheritdoc />
	public async Task ListenAsync()
	{
		try
		{
			if (!_serverLock.Wait(0))
				throw new InvalidOperationException(
					"Cannot start server, as it is already running or in the process of stopping.");
			CancellationToken token;
			try
			{
					if (!_stoppingTokenLock.Wait(0))
					throw new InvalidOperationException(
						"Cannot start server, as it is currently stopping."
					);
				Interlocked.Exchange<CancellationTokenSource?>(ref _stoppingTokenSource, new CancellationTokenSource());
				token = _stoppingTokenSource.Token;
			}
			finally
			{
				_stoppingTokenLock.Release();
			}
			_listeningSocket = _connectionFactory.CreateListeningSocket();
			_listeningSocket.Bind(new IPEndPoint(SocketEndpoints.Resolve(_address.Host), _address.Port));
			_listeningSocket.Listen();

			while (!token.IsCancellationRequested)
			{
				Socket? client = null;
				try
				{
					client = await _listeningSocket.AcceptAsync(token);
					_clientSockets.Add(client);
					_ = HandleConnectionAsync(_connectionFactory.CreateConnectionFromAcceptedSocket(client));
				}
				catch (Exception ex)
				{
					if (token.IsCancellationRequested)
					{
						if (ex is ObjectDisposedException || ex is SocketException)
						{
							break;
						}
					}
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
			_serverLock.Release();
		}

	}

	protected abstract Task HandleConnectionAsync(IConnection connection);

	protected virtual Task CleanupOnCloseAsync() => Task.CompletedTask;

	/// <summary>
	/// Closes the listening socket during shutdown.
	/// </summary>
	protected void CloseListeningSocket()
	{
		_listeningSocket?.Close();
		_listeningSocket = null;
	}
}
