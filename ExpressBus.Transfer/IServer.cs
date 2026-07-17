namespace ExpressBus.Transfer;

/// <summary>
/// Represents a server that accepts incoming connections.
/// </summary>
public interface IServer : IAsyncDisposable
{
	/// <summary>
	/// Starts accepting incoming connections.
	/// </summary>
	Task ListenAsync();
}
