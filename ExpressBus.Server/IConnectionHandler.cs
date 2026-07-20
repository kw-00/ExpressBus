namespace ExpressBus.Server;

public interface IConnectionHandler
{
    Task HandleConnection(Stream stream, CancellationToken cancellation);
}
