namespace ExpressBus.Transfer;

/// <summary>
/// Requests connections to remote endpoints.
/// Connections are created and returned by <see cref="ConnectAsync"/>.
/// </summary>
public interface IConnectionRequester
{
	/// <summary>
	/// Creates a new already-connected connection to the remote endpoint.
	/// </summary>
	/// <returns>A connected <see cref="IConnection"/> instance.</returns>
	Task<IConnection> ConnectAsync();
}
