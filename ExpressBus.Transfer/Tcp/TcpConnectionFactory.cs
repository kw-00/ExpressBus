using System.Net.Sockets;

namespace ExpressBus.Transfer.Tcp;

/// <summary>
/// Creates TCP sockets and wraps them as <see cref="TcpConnection"/> instances.
/// </summary>
public sealed class TcpConnectionFactory : IConnectionFactory
{
	/// <inheritdoc />
	public Socket CreateListeningSocket()
	{
		return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	}

	/// <inheritdoc />
	public IConnection CreateConnectionFromAcceptedSocket(Socket sock)
	{
		return new TcpConnection(sock);
	}

	/// <inheritdoc />
	public IConnection CreateConnection(Address address)
	{
		var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		return new TcpConnection(socket, address);
	}
}
