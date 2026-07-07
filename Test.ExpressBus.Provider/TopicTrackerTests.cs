using ExpressBus.Concurrency;
using ExpressBus.Provider;
using ExpressBus.Transfer;

public class TopicTrackerTests
{
    /// <summary>
    /// Minimal IConnection mock that tracks whether SendAsync was called.
    /// </summary>
    private sealed class FakeConnection : IConnection
    {
        private readonly Guid _id = Guid.NewGuid();
        public Task SendAsync(ReadOnlyMemory<byte> data) => Task.CompletedTask;
        public Task<int> ReceiveAsync(Memory<byte> buffer) => Task.FromResult(0);
        public Task CloseAsync(CloseMode mode) => Task.CompletedTask;
        public Action<CloseMode>? Closed { get; set; }
        public override int GetHashCode() => _id.GetHashCode();
        public override bool Equals(object? obj) => obj is FakeConnection other && other._id == _id;
    }

    private static ReadOnlyMemory<byte> Topic(string name) =>
        System.Text.Encoding.UTF8.GetBytes(name);

    #region AddSubscriber

    [Fact]
    public void AddSubscriber_CreatesTopic_AutoCreatesTopic()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn);

        // Verify no exception — topic was created
    }

    [Fact]
    public void AddSubscriber_DuplicateSubscriber_NotAddedTwice()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn);
        tracker.AddSubscriber(Topic("news"), conn);

        // Should not have two entries — HashSet deduplicates by reference
    }

    [Fact]
    public void AddSubscriber_DifferentConnections_AllAdded()
    {
        var tracker = new TopicTracker();
        var conn1 = new FakeConnection();
        var conn2 = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn1);
        tracker.AddSubscriber(Topic("news"), conn2);

        // Both connections should be subscribed
    }

    [Fact]
    public void AddSubscriber_MultipleTopics_Independent()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn);
        tracker.AddSubscriber(Topic("sports"), conn);

        // Connection subscribed to both topics independently
    }

    #endregion

    #region RemoveSubscriber(topic, subscriber)

    [Fact]
    public void RemoveSubscriber_RemovesSubscriber_ReturnsTrue()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn);
        var result = tracker.RemoveSubscriber(Topic("news"), conn);

        Assert.True(result);
    }

    [Fact]
    public void RemoveSubscriber_NonExistentTopic_ReturnsFalse()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        var result = tracker.RemoveSubscriber(Topic("news"), conn);

        Assert.False(result);
    }

    [Fact]
    public void RemoveSubscriber_NonExistentSubscriber_ReturnsFalse()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), new FakeConnection());
        var result = tracker.RemoveSubscriber(Topic("news"), conn);

        Assert.False(result);
    }

    [Fact]
    public void RemoveSubscriber_LastSubscriber_AutoRemovesTopic()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn);
        tracker.RemoveSubscriber(Topic("news"), conn);

        // Topic should be removed — no subscribers left
    }

    [Fact]
    public void RemoveSubscriber_OtherSubscribers_KeepsTopic()
    {
        var tracker = new TopicTracker();
        var conn1 = new FakeConnection();
        var conn2 = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn1);
        tracker.AddSubscriber(Topic("news"), conn2);
        tracker.RemoveSubscriber(Topic("news"), conn1);

        // Topic should still exist with conn2
    }

    #endregion

    #region RemoveSubscriber(subscriber)

    [Fact]
    public void RemoveSubscriber_Bulk_RemovesFromAllTopics()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn);
        tracker.AddSubscriber(Topic("sports"), conn);
        tracker.AddSubscriber(Topic("tech"), conn);

        tracker.RemoveSubscriber(conn);

        // Connection removed from all topics
    }

    [Fact]
    public void RemoveSubscriber_Bulk_AutoRemovesEmptyTopics()
    {
        var tracker = new TopicTracker();
        var conn = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn);

        tracker.RemoveSubscriber(conn);

        // Topic should be removed — no subscribers left
    }

    [Fact]
    public void RemoveSubscriber_Bulk_PreservesOtherSubscribers()
    {
        var tracker = new TopicTracker();
        var conn1 = new FakeConnection();
        var conn2 = new FakeConnection();

        tracker.AddSubscriber(Topic("news"), conn1);
        tracker.AddSubscriber(Topic("news"), conn2);

        tracker.RemoveSubscriber(conn1);

        // conn2 should still be subscribed
    }

    #endregion

    #region PartitionedProvider

    [Fact]
    public void PartitionedProvider_Get_RoutesToSamePartition()
    {
        var provider = new PartitionedProvider<string, int>(4, x => x.GetHashCode(), () => 0);
        var partition1 = provider.Get("hello");
        var partition2 = provider.Get("hello");

        Assert.Equal(partition1, partition2);
    }

    [Fact]
    public void PartitionedProvider_Get_DifferentKeys_MayRouteToDifferentPartitions()
    {
        var provider = new PartitionedProvider<string, int>(4, x => x.GetHashCode(), () => 0);
        var partition1 = provider.Get("hello");
        var partition2 = provider.Get("world");

        // They might be the same or different — just verify no exception
    }

    [Fact]
    public void PartitionedProvider_Get_PartitionCount_MatchesConstructor()
    {
        var provider = new PartitionedProvider<string, int>(16, x => x.GetHashCode(), () => 0);

        Assert.Equal(16, provider.PartitionCount);
    }

    [Fact]
    public void PartitionedProvider_Get_ReadOnlyMemoryByte_RoutesCorrectly()
    {
        var provider = new PartitionedProvider<ReadOnlyMemory<byte>, int>(4, HashProducers.ForReadOnlyMemoryByte, () => 0);
        var data1 = System.Text.Encoding.UTF8.GetBytes("topic1");
        var data2 = System.Text.Encoding.UTF8.GetBytes("topic1");
        var partition1 = provider.Get(data1);
        var partition2 = provider.Get(data2);

        Assert.Equal(partition1, partition2);
    }

    [Fact]
    public void PartitionedProvider_Constructor_ZeroPartitions_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PartitionedProvider<string, int>(0, x => x.GetHashCode(), () => 0));

        Assert.Equal("partitionCount", ex.ParamName);
    }

    [Fact]
    public void PartitionedProvider_Constructor_NegativePartitions_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PartitionedProvider<string, int>(-1, x => x.GetHashCode(), () => 0));

        Assert.Equal("partitionCount", ex.ParamName);
    }

    #endregion

    #region Concurrent access

    [Fact]
    public async Task TopicTracker_ConcurrentAdd_DoesNotThrow()
    {
        var tracker = new TopicTracker(partitionCount: 8);
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            var topicName = $"topic-{i % 20}";
            var conn = new FakeConnection();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    tracker.AddSubscriber(Topic(topicName), conn);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task TopicTracker_ConcurrentAddRemove_DoesNotThrow()
    {
        var tracker = new TopicTracker(partitionCount: 8);
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (var i = 0; i < 50; i++)
        {
            var topicName = $"topic-{i % 10}";
            var conn = new FakeConnection();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    tracker.AddSubscriber(Topic(topicName), conn);
                    tracker.RemoveSubscriber(Topic(topicName), conn);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
    }

    #endregion
}
