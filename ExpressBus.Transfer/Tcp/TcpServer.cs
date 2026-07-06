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
	protected override async Task ListenAsync(Action<IConnection> connectionHandler, CancellationToken cancellationToken)
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
				connectionHandler(new TcpConnection(accepted));
			}
			catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
			{
				// Expected during shutdown
				break;
			}
			catch (SocketException) when (cancellationToken.IsCancellationRequested)
			{
				// Expected during shutdown
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

	private static IPAddress GetIPAddress(string host)
	{
		if (IPAddress.TryParse(host, out var ip))
			return ip;

		var addresses = System.Net.Dns.GetHostAddresses(host);
		return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
			?? addresses.First();
	}
}
