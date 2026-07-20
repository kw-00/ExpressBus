namespace ExpressBus.Server;

public delegate Task ConnectionHandler(Stream stream, CancellationToken cancellation);
