namespace ExpressBus.Transfer;

/// <summary>
/// Represents a server that accepts incoming connections.
/// </summary>
public interface IServer
{
	/// <summary>
	/// Starts accepting incoming connections.
	/// </summary>
	Task ListenAsync();

	/// <summary>
	/// Stops the server and stops accepting new connections.
	/// </summary>
	Task StopAsync();
}
