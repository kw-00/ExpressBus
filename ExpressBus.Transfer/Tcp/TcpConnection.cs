using System.Net;
using System.Net.Sockets;

namespace ExpressBus.Transfer.Tcp;

/// <summary>
/// TCP socket implementation of <see cref="IConnection"/>.
/// Wraps a connected <see cref="Socket"/> for non-blocking byte-level I/O.
/// No framing — raw bytes are sent and received as-is.
/// </summary>
public sealed class TcpConnection : IConnection
{
	private readonly Socket _socket;
	private readonly object _lock = new();
	private bool _closed;

	/// <summary>
	/// Creates a <see cref="TcpConnection"/> wrapping an unconnected socket.
	/// </summary>
	/// <param name="socket">An unconnected socket.</param>
	public TcpConnection(Socket socket)
	{
		_socket = socket ?? throw new ArgumentNullException(nameof(socket));
	}

	/// <summary>
	/// Establishes the connection on this socket.
	/// </summary>
	internal async Task ConnectAsync(Address address)
	{
		var endpoint = new IPEndPoint(GetIPAddress(address.Host), address.Port);
		await _socket.ConnectAsync(endpoint);
	}

	/// <inheritdoc />
	public async Task SendAsync(ReadOnlyMemory<byte> data)
	{
		lock (_lock)
		{
			EnsureNotClosed();
		}

		int offset = 0;
		while (offset < data.Length)
		{
			int sent = await _socket.SendAsync(
				data.Slice(offset), SocketFlags.None, CancellationToken.None);
			offset += sent;
		}
	}

	/// <inheritdoc />
	public async Task<ReadOnlyMemory<byte>> ReceiveAsync()
	{
		var buffer = new byte[8192];
		var bytesRead = await _socket.ReceiveAsync(buffer, SocketFlags.None);

		if (bytesRead == 0)
		{
			CloseConnection(CloseMode.Shutdown);
			throw new IOException("Connection closed by remote end.");
		}

		return buffer.AsMemory(0, bytesRead);
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

	private static IPAddress GetIPAddress(string host) => SocketEndpoints.Resolve(host);
}
