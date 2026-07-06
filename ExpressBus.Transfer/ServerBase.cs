namespace ExpressBus.Transfer;

/// <summary>
/// Abstract base class that implements <see cref="IServer"/> and orchestrates
/// the protected <see cref="ListenAsync"/> and <see cref="HandleConnectionAsync"/> methods.
/// </summary>
public abstract class ServerBase : IServer
{
	private CancellationTokenSource? _stoppingToken;

	/// <inheritdoc />
	public async Task ListenAsync()
	{
		_stoppingToken = new CancellationTokenSource();
		var token = _stoppingToken.Token;
		await ListenAsync(connection =>
		{
			_ = Task.Run(() => HandleConnectionAsync(connection), token);
		}, token);
	}

	/// <inheritdoc />
	public virtual Task StopAsync()
	{
		_stoppingToken?.Cancel();
		_stoppingToken?.Dispose();
		_stoppingToken = null;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Called by <see cref="ListenAsync"/> to accept incoming connections.
	/// Each accepted connection is passed to <paramref name="connectionHandler"/>.
	/// </summary>
	/// <param name="connectionHandler">Callback invoked for each accepted connection.</param>
	/// <param name="cancellationToken">Token that signals the server should stop accepting.</param>
	protected abstract Task ListenAsync(Action<IConnection> connectionHandler, CancellationToken cancellationToken);

	/// <summary>
	/// Called for each accepted connection. Implement in a derived class to handle connection-specific logic.
	/// </summary>
	/// <param name="connection">The accepted connection.</param>
	protected abstract Task HandleConnectionAsync(IConnection connection);
}
