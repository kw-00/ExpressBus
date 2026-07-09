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
	/// <param name="cancellationToken">Optional cancellation token to abort the send operation.</param>
	/// <exception cref="Exception">Thrown when the connection is closed or an error occurs.</exception>
	Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

	/// <summary>
	/// Receives the next available bytes from the connection into the provided buffer.
	/// Returns raw bytes with no framing — callers are responsible for interpreting message boundaries.
	/// </summary>
	/// <param name="buffer">The buffer to fill with received bytes.</param>
	/// <param name="cancellationToken">Optional cancellation token to abort the receive operation.</param>
	/// <returns>The number of bytes actually written into <paramref name="buffer"/>.</returns>
	/// <exception cref="Exception">Thrown when the connection is closed or an error occurs.</exception>
	Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

	/// <summary>
	/// Continuously calls <see cref="ReceiveAsync"/> until the buffer is completely filled
	/// or the connection closes. Throws if the buffer cannot be filled.
	/// </summary>
	/// <param name="buffer">The buffer to fill with received bytes.</param>
	/// <param name="cancellationToken">Optional cancellation token to abort the receive operation.</param>
	/// <returns>The total number of bytes written into <paramref name="buffer"/> (always equal to <paramref name="buffer"/>.Length on success).</returns>
	/// <exception cref="IOException">Thrown when the connection closes before the buffer is full.</exception>
	/// <exception cref="Exception">Thrown when the connection is closed or an error occurs.</exception>
	Task<int> ReceiveFullAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

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
