using System.Net.Sockets;

namespace ExpressBus.Transfer.Tcp;

/// <summary>
/// TCP implementation of <see cref="IConnectionRequester"/>.
/// Connections created by <see cref="Connect"/> are already connected to the target address.
/// </summary>
public sealed class TcpConnectionRequester : IConnectionRequester
{
	private readonly Address _address;

	/// <summary>
	/// Creates a new <see cref="TcpConnectionRequester"/> bound to the given <paramref name="address"/>.
	/// </summary>
	/// <param name="address">The remote address to connect to.</param>
	public TcpConnectionRequester(Address address)
	{
		_address = address ?? throw new ArgumentNullException(nameof(address));
	}

	/// <inheritdoc />
	public async Task<IConnection> ConnectAsync()
	{
		var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		var connection = new TcpConnection(socket);
		await connection.ConnectAsync(_address);
		return connection;
	}
}
