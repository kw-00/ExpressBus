# ExpressBus

**High-performance binary pub/sub messaging for .NET 10.0.**

ExpressBus is a lightweight messaging protocol library that uses a publisher/subscriber (pub/sub) model over raw TCP. Message types are declared as `readonly partial struct` with declarative attributes, and C# Roslyn source generators produce zero-allocation binary serialization code at compile time.

The serialization wire format is minimal:

```
| byte MessageTypeIdentifier | N bytes of user fields... |
```

All multi-byte primitives use little-endian encoding via `BinaryPrimitives`.

---

## Key Features

- **Zero-allocation serialization** — `[Message]` and `[GenerateSerialization]` attributes on `readonly partial struct` types drive Roslyn incremental source generators that emit `ToBytes()` / `FromBytes()` implementations. No reflection, no boxing.
- **Raw TCP transport** — No HTTP, no gRPC, no framing overhead. Just bytes on a persistent socket connection.
- **Simple pub/sub** — Clients subscribe to byte-array topics, broadcast messages to topics, and receive `EventNotification` push notifications from the broker.
- **Stack-only binary I/O** — `ByteReader` and `ByteWriter` (`ref struct`) in `ExpressBus.Buffering` provide allocation-free reads and writes for `byte`, `bool`, `short`, `int`, `long`, `float`, `double`, and `Guid`.
- **Deterministic message type assignment** — All message types are sorted by fully-qualified name and assigned identifiers 0..N-1 at compile time. Maximum 256 types (byte-sized identifier).
- **.NET Generic Host integration** — The broker runs as an `IHostedService` (`Worker`) with clean lifecycle management.

---

## Architecture

```
ExpressBus.Protocol.SourceGeneration  (netstandard2.0)  — Roslyn source generators
         │
         ▼
ExpressBus.Protocol                   (net10.0)        — Core protocol: message types, handler bases, interfaces
         │
         ├──► ExpressBus.Buffering      (net10.0)        — ByteReader, ByteWriter, ByteTools (async stream helpers)
         ├──► ExpressBus.Transfer       (net10.0)        — IConnection, IConnectionFactory, TCP implementation
         └──► ExpressBus.Client         (net10.0)        — Client base class for broker communication
         │
ExpressBus.Provider                   (net10.0)        — TCP message broker (IHostedService)
```

### Projects

| Project | Role |
|---|---|
| `ExpressBus.Protocol` | Core protocol: message interfaces (`IMessage`, `IByteSerializable<T>`), bus message types (`BroadcastRequest`, `SubscribeRequest`, `EventNotification`, etc.), and handler base classes. |
| `ExpressBus.Protocol.SourceGeneration` | Roslyn source generators (`MessageGenerator`, `SerializableMemoryGenerator`) that emit serialization code and attribute declarations. |
| `ExpressBus.Buffering` | Stack-only binary I/O helpers (`ByteReader`, `ByteWriter`) and async stream utilities (`ByteTools`). |
| `ExpressBus.Transfer` | Transport abstraction: `IConnection` (send/receive/close), `IConnectionFactory` (socket creation), and the built-in TCP implementation (`TcpConnection`, `TcpConnectionFactory`). |
| `ExpressBus.Client` | `ExpressBusClientBase` — abstract client that handles subscription management, request/response round-trips, and notification dispatch. |
| `ExpressBus.Provider` | TCP message broker (`BrokerServer`) that accepts connections, tracks topic subscriptions, and dispatches notifications to subscribers. |

---

## Defining Messages

Declare a message type as a `readonly partial struct` and decorate it with `[Message]` and `[GenerateSerialization]`:

```csharp
using ExpressBus.Protocol;
using ExpressBus.Protocol.Bus;

[Message(MessageType.Request)]
[GenerateSerialization(MessageType.Request)]
[GenerateSerializedProp("RequestId", typeof(Guid))]
[GenerateSerializedProp("Topic", typeof(SerializableByteMemory))]
[GenerateSerializedProp("Message", typeof(SerializableByteMemory))]
public readonly partial struct MyCustomRequest : IMessage;
```

The source generator produces:
- An `IMessage` implementation with `MessageTypeIdentifier`
- A constructor that initializes all fields
- A `ByteSize` property for pre-allocation
- `ToBytes(Memory<byte>)` and static `FromBytes(Memory<byte>)` for serialization

**Supported field types:** `byte`, `bool`, `short`, `int`, `long`, `float`, `double`, `Guid`, enums (with any underlying integral type), and memory types (`SerializableByteMemory`, `SerializableBoolMemory`, `SerializableIntMemory`, `SerializableLongMemory`, `SerializableGuidMemory`).

---

## Running the Broker

The broker is a .NET Generic Host application that listens for TCP connections and routes messages:

```bash
dotnet run --project ExpressBus.Provider
```

The broker accepts client connections, tracks topic subscriptions per connection, and pushes `EventNotification` messages to all subscribers of a topic. When a client disconnects, its subscriptions are automatically cleaned up.

---

## Using the Client

The client library provides `ExpressBusClientBase` — an abstract class that manages a persistent TCP connection to the broker, handles subscription state, and dispatches incoming notifications to registered handlers.

### 1. Create a concrete client

Subclass `ExpressBusClientBase` and implement `EstablishConnection()` to create the transport connection:

```csharp
using ExpressBus.Client;
using ExpressBus.Transfer;
using ExpressBus.Transfer.Tcp;

public class MyClient : ExpressBusClientBase
{
    public MyClient(Address address) : base(address) { }

    protected override IConnection EstablishConnection()
    {
        var factory = new TcpConnectionFactory();
        return factory.CreateConnection(Address);
    }
}
```

### 2. Subscribe to a topic

Register a handler for a topic. Multiple handlers can be registered for the same topic. A broker-side subscription is sent only when the first handler is added:

```csharp
using var client = new MyClient(new Address("localhost", 5000));

await client.SubscribeAsync(
    topic: Encoding.UTF8.GetBytes("orders.created"),
    handler: notification =>
    {
        var orderData = JsonSerializer.Deserialize<Order>(notification);
        Console.WriteLine($"Received order: {orderData?.Id}");
    });
```

### 3. Broadcast a message

Send a message to all subscribers of a topic:

```csharp
var message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Id = 42, Item = "Widget" }));
await client.BroadcastAsync(
    topic: Encoding.UTF8.GetBytes("orders.created"),
    message: message);
```

### 4. Unsubscribe

Remove all handlers for a topic. A broker-side unsubscribe is sent only when the last handler is removed:

```csharp
await client.UnsubscribeAsync(Encoding.UTF8.GetBytes("orders.created"));
```

### 5. Disposal

Use `await using` to ensure clean shutdown. Disposal sends unsubscribe requests for all tracked topics and closes the connection:

```csharp
await using var client = new MyClient(new Address("localhost", 5000));
// ... use client ...
// On disposal: unsubscribes from all topics and closes the connection
```

---

## Extensibility

ExpressBus is designed to work over any persistent-connection transport built on OS sockets. The transport layer is fully abstracted behind two interfaces:

### `IConnectionFactory` (server-side)

The broker uses `IConnectionFactory` to create listening sockets and wrap accepted connections:

```csharp
public interface IConnectionFactory
{
    Socket CreateListeningSocket();
    IConnection CreateConnectionFromAcceptedSocket(Socket sock);
    IConnection CreateConnection(Address address);
}
```

Replace the default `TcpConnectionFactory` to customize socket options, enable TLS, or implement a different protocol.

### `ExpressBusClientBase.EstablishConnection()` (client-side)

Each client implements this abstract method to return an `IConnection`. The connection must support async send/receive, a `CloseAsync` method, and a `Closed` event.

### SSL/TLS

To add SSL/TLS, create a connection factory or client connection that wraps the socket in `SslStream`:

```csharp
public class SslConnectionFactory : IConnectionFactory
{
    public Socket CreateListeningSocket() =>
        new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public IConnection CreateConnectionFromAcceptedSocket(Socket sock)
    {
        var sslStream = new SslStream(new NetworkStream(sock), leaveInnerStreamOpen: false);
        return new SslConnection(sslStream);  // your IConnection implementation
    }

    public IConnection CreateConnection(Address address)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(new IPEndPoint(SocketEndpoints.Resolve(address.Host), address.Port));
        var sslStream = new SslStream(new NetworkStream(socket), leaveInnerStreamOpen: false);
        sslStream.AuthenticateAsClient(new SslClientAuthenticationOptions { /* config */ });
        return new SslConnection(sslStream);
    }
}
```

### WebSocket

Implement `IConnection` over `System.Net.WebSockets.ClientWebSocket`, framing WebSocket frames around the raw ExpressBus binary payloads. The broker side would need a matching WebSocket-aware `IConnectionFactory`.

### Custom Protocols

Any protocol that provides:
1. A persistent connection (single socket, no per-message reconnection)
2. Async byte-level send/receive
3. A close/disconnect signal (`Closed` event)

...can serve as an ExpressBus transport. The protocol layer only sees raw bytes — ExpressBus handles nothing at the transport level.

---

## Wire Format

### Request

```
| 1 byte: MessageTypeIdentifier | N bytes: serialized payload (ToBytes) |
```

### Response

```
| 1 byte: MessageTypeIdentifier | 4 bytes: payload size (little-endian int32) | N bytes: serialized payload (ToBytes) |
```

### Notification (push)

```
| 1 byte: MessageTypeIdentifier | N bytes: serialized payload (ToBytes) |
```

The single-byte type identifier enables O(1) dispatch on the wire. Response messages include a 4-byte length prefix so the receiver knows exactly how many bytes to read for the payload.

---

## Building and Testing

```bash
# Build the entire solution
dotnet build

# Run tests
dotnet test

# Run the provider (message broker)
dotnet run --project ExpressBus.Provider
```
