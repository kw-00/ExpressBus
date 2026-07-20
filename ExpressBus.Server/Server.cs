using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace ExpressBus.Server;

public class Server : IServer
{
    private readonly IPEndPoint _address;
    private readonly ConnectionHandler _connectionHandler;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public Server(IPEndPoint address, ConnectionHandler connectionHandler)
    {
        _address = address;
        _connectionHandler = connectionHandler;
    }

    public async Task ListenAsync()
    {
        if (_cancellation.IsCancellationRequested)
        {
            throw new ObjectDisposedException(nameof(Server));
        }

        Socket? serverSocket = null;
        if (!_lock.Wait(0)) throw new InvalidOperationException("Server is already running.");
        try
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(_address);
            serverSocket.Listen();

            while (!_cancellation.IsCancellationRequested)
            {
                var clientSocket = await serverSocket.AcceptAsync();
                var networkStream = new NetworkStream(clientSocket);
                var sslStream = new SslStream(networkStream);
                _ = _connectionHandler(sslStream, _cancellation.Token);
            }

        }
        finally
        {
            if (serverSocket is not null)
            {
                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();
            }
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        await _lock.WaitAsync();
        _lock.Dispose();
    }
}
