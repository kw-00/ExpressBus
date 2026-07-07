namespace ExpressBus.Transfer;

/// <summary>
/// Represents a persistent connection to a remote endpoint.
/// </summary>
public interface IConnection
{
	/// <summary>
	/// Sends a frame of bytes over the connection.
	/// </summary>
	/// <param name="data">The bytes to send.</param>
	/// <exception cref="Exception">Thrown when the connection is closed or an error occurs.</exception>
	Task SendAsync(ReadOnlyMemory<byte> data);

	/// <summary>
	/// Receives the next available bytes from the connection into the provided buffer.
	/// Returns raw bytes with no framing — callers are responsible for interpreting message boundaries.
	/// </summary>
	/// <param name="buffer">The buffer to fill with received bytes.</param>
	/// <returns>The number of bytes actually written into <paramref name="buffer"/>.</returns>
	/// <exception cref="Exception">Thrown when the connection is closed or an error occurs.</exception>
	Task<int> ReceiveAsync(Memory<byte> buffer);

	/// <summary>
	/// Closes the connection using the specified mode.
	/// </summary>
	/// <param name="mode">How to close the connection.</param>
	Task CloseAsync(CloseMode mode);

	/// <summary>
	/// Invoked when the connection is closed, passing the close mode.
	/// </summary>
	Action<CloseMode>? Closed { get; set; }
}
