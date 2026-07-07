using System.Net.Sockets;

namespace ExpressBus.Transfer.Tcp;

/// <summary>
/// TCP server that accepts incoming connections and wraps them as <see cref="TcpConnection"/> instances.
/// Inherits the listening/accepting infrastructure from <see cref="ServerBase"/> and implements
/// the socket-specific abstract methods.
/// </summary>
public abstract class TcpServer : ServerBase
{
	/// <summary>
	/// Creates a new <see cref="TcpServer"/> bound to the given <paramref name="address"/>.
	/// </summary>
	/// <param name="address">The address to bind and listen on.</param>
	protected TcpServer(Address address)
		: base(address)
	{
	}

	/// <inheritdoc />
	protected override Socket CreateListeningSocket()
	{
		return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	}

	/// <inheritdoc />
	protected override void ConfigureClientSocket(Socket clientSocket)
	{
	}

	/// <inheritdoc />
	protected override IConnection CreateConnection(Socket clientSocket)
	{
		return new TcpConnection(clientSocket);
	}
}
