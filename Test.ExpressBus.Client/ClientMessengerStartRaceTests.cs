using System.Net.Sockets;
using System.Threading;
using ExpressBus.Client;
using ExpressBus.Transfer;

namespace Test.ExpressBus.Client;

public class ClientMessengerStartRaceTests
{
    /// <summary>
    /// Concurrent calls to ClientMessenger.StartAsync() must create exactly one connection.
    /// </summary>
    [Fact]
    public async Task ClientMessenger_double_start_creates_single_connection()
    {
        var factory = new GateableConnectionFactory();
        var messenger = new ClientMessenger(factory, new Address("localhost", 5000));

        var startGate = new ManualResetEventSlim(false);
        const int concurrency = 10;
        var tasks = new List<Task>();

        for (int i = 0; i < concurrency; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                startGate.Wait();
                await messenger.StartAsync();
            }));
        }

        // All 10 threads are now blocked on startGate, about to call StartAsync()
        startGate.Set();

        // Let the factory produce connections (only one should be requested)
        factory.CreateConnectionGate.Set();

        await Task.WhenAll(tasks);
        await messenger.DisposeAsync();

        Assert.Equal(1, factory.ConnectionCount);
    }

    /// <summary>
    /// Stress test: many rounds of concurrent StartAsync() calls must never create more
    /// than one connection per round.
    /// </summary>
    [Fact]
    public async Task ClientMessenger_concurrent_starts_stress_test()
    {
        const int rounds = 50;
        const int concurrency = 8;

        for (int round = 0; round < rounds; round++)
        {
            var factory = new GateableConnectionFactory();
            var messenger = new ClientMessenger(factory, new Address("localhost", 5000));

            var startGate = new ManualResetEventSlim(false);
            var tasks = new List<Task>();

            for (int i = 0; i < concurrency; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    startGate.Wait();
                    await messenger.StartAsync();
                }));
            }

            startGate.Set();
            factory.CreateConnectionGate.Set();

            await Task.WhenAll(tasks);
            await messenger.DisposeAsync();

            Assert.Equal(1, factory.ConnectionCount);
        }
    }

    /// <summary>
    /// Concurrent calls to ExpressBusClient.StartAsync() must create exactly one connection.
    /// Note: the write lock in ExpressBusClient serializes callers, so the _started guard
    /// being outside the lock (old code) is a correctness improvement but not a leak vector
    /// in practice — the lock prevents double-creation regardless.
    /// </summary>
    [Fact]
    public async Task ExpressBusClient_double_start_creates_single_connection()
    {
        var factory = new GateableConnectionFactory();
        var client = new ExpressBusClient(factory, new Address("localhost", 5000));

        var startGate = new ManualResetEventSlim(false);
        const int concurrency = 10;
        var tasks = new List<Task>();

        for (int i = 0; i < concurrency; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                startGate.Wait();
                await client.StartAsync();
            }));
        }

        startGate.Set();
        factory.CreateConnectionGate.Set();

        await Task.WhenAll(tasks);
        await client.DisposeAsync();

        Assert.Equal(1, factory.ConnectionCount);
    }

    /// <summary>
    /// If StartAsync() fails, a subsequent call should be able to succeed.
    /// </summary>
    [Fact]
    public async Task ClientMessenger_start_failure_allows_retry()
    {
        var factory = new FailingThenSucceedingConnectionFactory();
        var messenger = new ClientMessenger(factory, new Address("localhost", 5000));

        // First call should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await messenger.StartAsync());

        Assert.Equal(0, factory.ConnectionCount);

        // Second call should succeed
        await messenger.StartAsync();
        Assert.Equal(1, factory.ConnectionCount);

        await messenger.DisposeAsync();
    }

    /// <summary>
    /// DisposeAsync should wait for the listener task to complete.
    /// </summary>
    [Fact]
    public async Task ClientMessenger_dispose_awaits_listener_task()
    {
        var factory = new DelayedReceiveConnectionFactory();
        var messenger = new ClientMessenger(factory, new Address("localhost", 5000));

        await messenger.StartAsync();

        // Dispose should not complete until the listener has exited
        var disposeTask = messenger.DisposeAsync().AsTask();
        Assert.False(disposeTask.IsCompleted);

        // Let the listener receive one frame then fail (IOException on second read)
        factory.ReceiveGate.Release();

        await disposeTask;

        Assert.True(disposeTask.IsCompletedSuccessfully);
    }

    // ─── Mock: Factory that gates CreateConnection and counts invocations ───

    private sealed class GateableConnectionFactory : IConnectionFactory
    {
        public readonly ManualResetEventSlim CreateConnectionGate = new(false);
        public int ConnectionCount;

        public Socket CreateListeningSocket() => throw new NotSupportedException();
        public IConnection CreateConnectionFromAcceptedSocket(Socket sock) => throw new NotSupportedException();

        public IConnection CreateConnection(Address address)
        {
            CreateConnectionGate.Wait();
            Interlocked.Increment(ref ConnectionCount);
            return new MockConnection();
        }
    }

    // ─── Mock: Factory that fails once then succeeds ───

    private sealed class FailingThenSucceedingConnectionFactory : IConnectionFactory
    {
        public int CallCount;
        public int ConnectionCount;

        public Socket CreateListeningSocket() => throw new NotSupportedException();
        public IConnection CreateConnectionFromAcceptedSocket(Socket sock) => throw new NotSupportedException();

        public IConnection CreateConnection(Address address)
        {
            if (Interlocked.Increment(ref CallCount) == 1)
                throw new InvalidOperationException("Simulated failure");
            Interlocked.Increment(ref ConnectionCount);
            return new MockConnection();
        }
    }

    // ─── Mock: Factory whose connection delays the first receive ───

    private sealed class DelayedReceiveConnectionFactory : IConnectionFactory
    {
        public readonly SemaphoreSlim ReceiveGate = new(0, 1);

        public Socket CreateListeningSocket() => throw new NotSupportedException();
        public IConnection CreateConnectionFromAcceptedSocket(Socket sock) => throw new NotSupportedException();
        public IConnection CreateConnection(Address address) => new DelayedReceiveConnection(ReceiveGate);
    }

    // ─── Mock: Connection that immediately fails receives (listener exits) ───

    private sealed class MockConnection : IConnection
    {
        public Action<CloseMode>? Closed { get; set; }

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new IOException("Connection closed");

        public Task<int> ReceiveFullAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new IOException("Connection closed");

        public Task CloseAsync(CloseMode mode)
        {
            Closed?.Invoke(mode);
            return Task.CompletedTask;
        }
    }

    // ─── Mock: Connection that gates the first receive, then fails ───

    private sealed class DelayedReceiveConnection : IConnection
    {
        private readonly SemaphoreSlim _gate;
        private int _receiveCount;

        public DelayedReceiveConnection(SemaphoreSlim gate) => _gate = gate;
        public Action<CloseMode>? Closed { get; set; }

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new IOException("Connection closed");

        public async Task<int> ReceiveFullAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _receiveCount) == 1)
            {
                await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            throw new IOException("Connection closed");
        }

        public Task CloseAsync(CloseMode mode)
        {
            Closed?.Invoke(mode);
            return Task.CompletedTask;
        }
    }
}
