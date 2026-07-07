using System.Net;
using System.Net.Sockets;

namespace ExpressBus.Transfer.Tcp;

/// <summary>
/// Abstract TCP server that accepts incoming connections and wraps them as <see cref="IConnection"/>.
/// Leaves <see cref="HandleConnectionAsync"/> unimplemented — concrete subclasses handle per-connection logic.
/// </summary>
public abstract class ServerBase : IServer 
{
	private readonly Address _address;
	private HashSet<Socket> _clientSockets = new();
	private Socket? _listeningSocket;

	private CancellationTokenSource? _stoppingTokenSource;
	private readonly SemaphoreSlim _stoppingTokenLock = new SemaphoreSlim(1, 1);

	private readonly SemaphoreSlim _serverRunLock = new SemaphoreSlim(1, 1);

	/// <summary>
	/// Creates a new <see cref="ServerBase"/> bound to the given <paramref name="address"/>.
	/// </summary>
	/// <param name="address">The address to bind and listen on.</param>
	public ServerBase(Address address)
	{
		_address = address ?? throw new ArgumentNullException(nameof(address));
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
			CloseListeningSocket();
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
			var serverAlreadyRunning = !_serverRunLock.Wait(0);
			if (serverAlreadyRunning)
				throw new InvalidOperationException("Cannot start server, as it is already running.");
			CancellationToken token;
			try
			{
				await _stoppingTokenLock.WaitAsync();
				Interlocked.Exchange<CancellationTokenSource?>(ref _stoppingTokenSource, new CancellationTokenSource());
				token = _stoppingTokenSource.Token;
			}
			finally
			{
				_stoppingTokenLock.Release();
			}
			_listeningSocket = CreateListeningSocket();
			_listeningSocket.Bind(new IPEndPoint(SocketEndpoints.Resolve(_address.Host), _address.Port));
			_listeningSocket.Listen();

			while (!token.IsCancellationRequested)
			{
				Socket? client = null;
				try
				{
					client = await _listeningSocket.AcceptAsync(token);
					_clientSockets.Add(client);
					ConfigureClientSocket(client);
					_ = HandleConnectionAsync(CreateConnection(client));
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
			_serverRunLock.Release();
		}

	}

	protected abstract Socket CreateListeningSocket();

	protected abstract void ConfigureClientSocket(Socket clientSocket);

	protected abstract IConnection CreateConnection(Socket clientSocket);

	protected abstract Task HandleConnectionAsync(IConnection connection);

	/// <summary>
	/// Closes the listening socket during shutdown.
	/// </summary>
	protected void CloseListeningSocket()
	{
		_listeningSocket?.Close();
		_listeningSocket = null;
	}
}
