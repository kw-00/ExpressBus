using System.Net.Sockets;

namespace ExpressBus.Transfer;

/// <summary>
/// Factory for creating network sockets and wrapping them as <see cref="IConnection"/> instances.
/// </summary>
/// <remarks>
/// Decouples <see cref="ServerBase"/> from transport-specific socket creation.
/// Concrete implementations (e.g. <see cref="Tcp.TcpConnectionFactory"/>) customize
/// socket type, address family, and protocol.
/// </remarks>
public interface IConnectionFactory
{
	/// <summary>
	/// Creates a socket configured for listening (bound and ready to accept connections).
	/// </summary>
	Socket CreateListeningSocket();

	/// <summary>
	/// Wraps a socket accepted from a listening socket into an <see cref="IConnection"/>.
	/// </summary>
	/// <param name="sock">The accepted socket.</param>
	IConnection CreateConnectionFromAcceptedSocket(Socket sock);

	/// <summary>
	/// Creates an outbound <see cref="IConnection"/> by connecting to the given remote address.
	/// </summary>
	/// <param name="address">The remote address to connect to.</param>
	IConnection CreateConnection(Address address);
}
