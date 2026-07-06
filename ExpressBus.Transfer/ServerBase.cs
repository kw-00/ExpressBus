namespace ExpressBus.Transfer;

/// <summary>
/// Abstract base class that implements <see cref="IServer"/> and orchestrates
/// the protected <see cref="ListenAsync"/> and <see cref="HandleConnectionAsync"/> methods.
/// </summary>
public abstract class ServerBase : IServer
{
	private CancellationTokenSource? _stoppingToken;
	private Task? _listenTask;

	/// <inheritdoc />
	public async Task ListenAsync()
	{
		_stoppingToken = new CancellationTokenSource();
		var token = _stoppingToken.Token;
		_listenTask = ListenAsync(HandleConnectionAsync, token);
		await _listenTask;
	}

	/// <inheritdoc />
	public async Task StopAsync()
	{
		var cts = _stoppingToken;
		_stoppingToken = null;
		cts?.Cancel();
		cts?.Dispose();

		var task = _listenTask;
		_listenTask = null;
		if (task is not null)
			await task.ConfigureAwait(false);
	}

	/// <summary>
	/// Called by <see cref="ListenAsync"/> to accept incoming connections.
	/// Each accepted connection is passed to <paramref name="connectionHandler"/>.
	/// </summary>
	/// <param name="connectionHandler">Callback invoked for each accepted connection. Returns a task representing the connection lifetime.</param>
	/// <param name="cancellationToken">Token that signals the server should stop accepting.</param>
	/// <returns>A task representing the lifetime of the accept loop.</returns>
	protected abstract Task ListenAsync(Func<IConnection, Task> connectionHandler, CancellationToken cancellationToken);

	/// <summary>
	/// Called for each accepted connection. Implement in a derived class to handle connection-specific logic.
	/// </summary>
	/// <param name="connection">The accepted connection.</param>
	protected abstract Task HandleConnectionAsync(IConnection connection);
}
