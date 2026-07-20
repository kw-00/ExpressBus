namespace ExpressBus.Server;

public interface IServer : IAsyncDisposable
{
    Task ListenAsync();
}
