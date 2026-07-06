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

	/// <summary>
	/// Creates a new <see cref="TcpServer"/> bound to the given <paramref name="address"/>.
	/// </summary>
	/// <param name="address">The address to bind and listen on.</param>
	public TcpServer(Address address)
	{
		_address = address ?? throw new ArgumentNullException(nameof(address));
	}

	/// <inheritdoc />
	protected override async Task ListenAsync(Func<IConnection, Task> connectionHandler, CancellationToken cancellationToken)
	{
		_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		_listeningSocket.Bind(new IPEndPoint(GetIPAddress(_address.Host), _address.Port));
		_listeningSocket.Listen();

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var accepted = await _listeningSocket.AcceptAsync();
				accepted.NoDelay = true;
				await connectionHandler(new TcpConnection(accepted));
			}
			catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			catch (SocketException) when (cancellationToken.IsCancellationRequested)
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

	private static IPAddress GetIPAddress(string host) => SocketEndpoints.Resolve(host);
}
