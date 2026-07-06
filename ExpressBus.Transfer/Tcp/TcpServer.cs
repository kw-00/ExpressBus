using System.Net;
using System.Net.Sockets;

namespace ExpressBus.Transfer.Tcp;

/// <summary>
/// Abstract TCP server that accepts incoming connections and wraps them as <see cref="IConnection"/>.
/// Leaves <see cref="HandleConnectionAsync"/> unimplemented — concrete subclasses handle per-connection logic.
/// </summary>
public abstract class TcpServer : ServerBase
{
	private readonly Address _address;
	private Socket? _listeningSocket;
	private CancellationTokenSource? _stoppingToken;

	/// <summary>
	/// Creates a new <see cref="TcpServer"/> bound to the given <paramref name="address"/>.
	/// </summary>
	/// <param name="address">The address to bind and listen on.</param>
	public TcpServer(Address address)
	{
		_address = address ?? throw new ArgumentNullException(nameof(address));
	}

	/// <inheritdoc />
	public override void StopAsync()
	{
		CloseListeningSocket();

		var cts = _stoppingToken;
		_stoppingToken = null;
		cts?.Cancel();
		cts?.Dispose();
	}

	/// <inheritdoc />
	protected override async Task RunAcceptLoop(Func<IConnection, Task> connectionHandler)
	{
		_stoppingToken = new CancellationTokenSource();
		var token = _stoppingToken.Token;

		_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		_listeningSocket.Bind(new IPEndPoint(SocketEndpoints.Resolve(_address.Host), _address.Port));
		_listeningSocket.Listen();

		while (!token.IsCancellationRequested)
		{
			try
			{
				var accepted = await _listeningSocket.AcceptAsync();
				accepted.NoDelay = true;
				await connectionHandler(new TcpConnection(accepted));
			}
			catch (ObjectDisposedException) when (token.IsCancellationRequested)
			{
				break;
			}
			catch (SocketException) when (token.IsCancellationRequested)
			{
				break;
			}
		}
	}

	/// <summary>
	/// Closes the listening socket during shutdown.
	/// </summary>
	protected void CloseListeningSocket()
	{
		_listeningSocket?.Close();
		_listeningSocket = null;
	}
}
