using System.Net.Sockets;

namespace ExpressBus.Transfer.Tcp;

/// <summary>
/// TCP socket implementation of <see cref="IConnection"/>.
/// Wraps a connected <see cref="Socket"/> for non-blocking byte-level I/O.
/// No framing — raw bytes are sent and received as-is.
/// </summary>
internal sealed class TcpConnection : IConnection
{
	private readonly Socket _socket;
	private readonly object _lock = new();
	private bool _closed;

	/// <summary>
	/// Creates a <see cref="TcpConnection"/> wrapping a pre-connected socket (incoming connection).
	/// </summary>
	/// <param name="socket">A connected TCP socket, returned from <see cref="Socket.AcceptAsync"/>.</param>
	public TcpConnection(Socket socket)
	{
		ValidateTcpSocket(socket, expectConnected: true);
		_socket = socket;
	}

	/// <summary>
	/// Creates a <see cref="TcpConnection"/> and connects an unconnected socket to the target address (outgoing connection).
	/// </summary>
	/// <param name="socket">An unconnected TCP socket.</param>
	/// <param name="address">The remote address to connect to.</param>
	public TcpConnection(Socket socket, Address address)
	{
		ValidateTcpSocket(socket, expectConnected: false);
		_socket = socket;
		var endpoint = new System.Net.IPEndPoint(SocketEndpoints.Resolve(address.Host), address.Port);
		_socket.Connect(endpoint);
	}

	private static void ValidateTcpSocket(Socket socket, bool expectConnected)
	{
		if (socket == null)
			throw new ArgumentNullException(nameof(socket));

		if (socket.AddressFamily != AddressFamily.InterNetwork)
			throw new ArgumentException("Socket must use IPv4 address family.", nameof(socket));

		if (socket.SocketType != SocketType.Stream)
			throw new ArgumentException("Socket must be a stream (TCP) socket.", nameof(socket));

		if (socket.ProtocolType != ProtocolType.Tcp)
			throw new ArgumentException("Socket must use the TCP protocol.", nameof(socket));

		var isConnected = socket.Connected;
		if (expectConnected && !isConnected)
			throw new ArgumentException("Socket must be pre-connected (use the two-argument constructor for outgoing connections).", nameof(socket));

		if (!expectConnected && isConnected)
			throw new ArgumentException("Socket must be unconnected (use the single-argument constructor for incoming connections).", nameof(socket));
	}

	/// <inheritdoc />
	public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			EnsureNotClosed();
		}

		await _socket.SendAsync(data, SocketFlags.None, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		var bytesRead = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);

		if (bytesRead == 0)
		{
			CloseConnection(CloseMode.Shutdown);
			throw new IOException("Connection closed by remote end.");
		}

		return bytesRead;
	}

	/// <inheritdoc />
	public async Task<int> ReceiveFullAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		int totalRead = 0;
		while (totalRead < buffer.Length)
		{
			var bytesRead = await ReceiveAsync(buffer.Slice(totalRead), cancellationToken).ConfigureAwait(false);
			totalRead += bytesRead;
		}
		return totalRead;
	}

	/// <inheritdoc />
	public Task CloseAsync(CloseMode mode)
	{
		CloseConnection(mode);
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Action<CloseMode>? Closed { get; set; }

	private void CloseConnection(CloseMode mode)
	{
		lock (_lock)
		{
			if (_closed)
				return;

			_closed = true;

			if (mode == CloseMode.Shutdown)
			{
				try
				{
					_socket.Shutdown(SocketShutdown.Both);
				}
				catch
				{
					// Already closed or disconnected — ignore
				}
			}

			try
			{
				_socket.Close();
			}
			catch
			{
				// Ignore close errors
			}

			Closed?.Invoke(mode);
		}
	}

	private void EnsureNotClosed()
	{
		if (_closed)
			throw new IOException("Connection is closed.");
	}

	private static System.Net.IPAddress Resolve(string host) => SocketEndpoints.Resolve(host);
}
