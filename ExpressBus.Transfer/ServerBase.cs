namespace ExpressBus.Transfer;

/// <summary>
/// Abstract base class that orchestrates the protected <see cref="RunAcceptLoop"/>
/// and <see cref="HandleConnectionAsync"/> methods.
/// Cancellation is handled by derived classes.
/// </summary>
public abstract class ServerBase : IServer
{
	/// <inheritdoc />
	public virtual Task ListenAsync() => RunAcceptLoop(HandleConnectionAsync);

	/// <inheritdoc />
	public abstract void StopAsync();

	/// <summary>
	/// Runs the accept loop for incoming connections.
	/// Derived classes configure their own cancellation and implement the loop body.
	/// </summary>
	/// <param name="connectionHandler">Callback invoked for each accepted connection.</param>
	protected abstract Task RunAcceptLoop(Func<IConnection, Task> connectionHandler);

	/// <summary>
	/// Called for each accepted connection. Implement in a derived class to handle connection-specific logic.
	/// </summary>
	/// <param name="connection">The accepted connection.</param>
	protected abstract Task HandleConnectionAsync(IConnection connection);
}
