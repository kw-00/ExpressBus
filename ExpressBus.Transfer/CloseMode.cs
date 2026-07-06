namespace ExpressBus.Transfer;

/// <summary>
/// Specifies how a connection should be closed.
/// </summary>
public enum CloseMode
{
	/// <summary>
	/// Graceful shutdown. Allows any outstanding data to be flushed before closing.
	/// </summary>
	Shutdown,

	/// <summary>
	/// Force close. Drops the connection immediately without a graceful shutdown.
	/// </summary>
	Destroy,
}
