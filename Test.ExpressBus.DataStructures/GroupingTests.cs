using ExpressBus.DataStructures;

public class GroupingTests
{
    private static readonly MemoryComparer<byte> Comparer = MemoryComparer<byte>.Instance;
    private static readonly Func<ReadOnlyMemory<byte>, int> HashFunc = HashProducers.ForReadOnlyMemoryByte;

    private static ReadOnlyMemory<byte> Topic(string name) =>
        System.Text.Encoding.UTF8.GetBytes(name);

    #region Add

    [Fact]
    public void Add_CreatesGroup_AutoCreatesGroup()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        grouping.Add(topic, "handler1");

        var handlers = grouping.Get(topic);
        Assert.Single(handlers);
        Assert.Contains("handler1", handlers);
    }

    [Fact]
    public void Add_DuplicateValue_NotAddedTwice()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        grouping.Add(topic, "handler1");
        grouping.Add(topic, "handler1");

        var handlers = grouping.Get(topic);
        Assert.Single(handlers);
    }

    [Fact]
    public void Add_DifferentValues_AllAdded()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        grouping.Add(topic, "handler1");
        grouping.Add(topic, "handler2");
        grouping.Add(topic, "handler3");

        var handlers = grouping.Get(topic);
        Assert.Equal(3, handlers.Count);
    }

    [Fact]
    public void Add_MultipleTopics_Independent()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);

        grouping.Add(Topic("news"), "handler1");
        grouping.Add(Topic("sports"), "handler2");

        var newsHandlers = grouping.Get(Topic("news"));
        var sportsHandlers = grouping.Get(Topic("sports"));
        Assert.Single(newsHandlers);
        Assert.Single(sportsHandlers);
        Assert.Contains("handler1", newsHandlers);
        Assert.Contains("handler2", sportsHandlers);
    }

    #endregion

    #region Remove(group, value)

    [Fact]
    public void Remove_ReturnsTrue_WhenFound()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        grouping.Add(topic, "handler1");
        var result = grouping.Remove(topic, "handler1");

        Assert.True(result);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenGroupMissing()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        var result = grouping.Remove(topic, "handler1");

        Assert.False(result);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenValueMissing()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        grouping.Add(topic, "handler1");
        var result = grouping.Remove(topic, "handler2");

        Assert.False(result);
    }

    [Fact]
    public void Remove_LastValue_AutoRemovesGroup()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        grouping.Add(topic, "handler1");
        grouping.Remove(topic, "handler1");

        var handlers = grouping.Get(topic);
        Assert.Empty(handlers);
    }

    [Fact]
    public void Remove_OtherValues_KeepsGroup()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        grouping.Add(topic, "handler1");
        grouping.Add(topic, "handler2");
        grouping.Remove(topic, "handler1");

        var handlers = grouping.Get(topic);
        Assert.Single(handlers);
        Assert.Contains("handler2", handlers);
    }

    #endregion

    #region RemoveAll(value)

    [Fact]
    public void RemoveAll_RemovesFromAllGroups()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);

        grouping.Add(Topic("news"), "shared");
        grouping.Add(Topic("sports"), "shared");
        grouping.Add(Topic("tech"), "shared");

        grouping.RemoveAll("shared");

        Assert.Empty(grouping.Get(Topic("news")));
        Assert.Empty(grouping.Get(Topic("sports")));
        Assert.Empty(grouping.Get(Topic("tech")));
    }

    [Fact]
    public void RemoveAll_AutoRemovesEmptyGroups()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);

        grouping.Add(Topic("news"), "shared");

        grouping.RemoveAll("shared");

        Assert.Empty(grouping.Get(Topic("news")));
    }

    [Fact]
    public void RemoveAll_PreservesOtherValues()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);

        grouping.Add(Topic("news"), "shared");
        grouping.Add(Topic("news"), "unique");

        grouping.RemoveAll("shared");

        var handlers = grouping.Get(Topic("news"));
        Assert.Single(handlers);
        Assert.Contains("unique", handlers);
    }

    [Fact]
    public void RemoveAll_ValueNotPresent_NoEffect()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);

        grouping.Add(Topic("news"), "handler1");

        grouping.RemoveAll("nonexistent");

        var handlers = grouping.Get(Topic("news"));
        Assert.Single(handlers);
    }

    #endregion

    #region Get

    [Fact]
    public void Get_ReturnsEmptySet_WhenGroupMissing()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);

        var handlers = grouping.Get(Topic("news"));

        Assert.Empty(handlers);
    }

    [Fact]
    public void Get_ReturnsCopy_NotReference()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic = Topic("news");

        grouping.Add(topic, "handler1");

        var copy1 = grouping.Get(topic);
        var copy2 = grouping.Get(topic);

        copy1.Add("injected");
        Assert.DoesNotContain("injected", grouping.Get(topic));
    }

    #endregion

    #region Content-based comparison

    [Fact]
    public void Add_WithSameContent_DifferentArrays_SameGroup()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, string>(Comparer, HashFunc);
        var topic1 = Topic("news");
        var topic2 = System.Text.Encoding.UTF8.GetBytes("news");

        grouping.Add(topic1, "handler1");
        grouping.Add(topic2, "handler2");

        var handlers = grouping.Get(topic1);
        Assert.Equal(2, handlers.Count);
    }

    #endregion

    #region Concurrent access

    [Fact]
    public async Task ConcurrentAdd_DoesNotThrow()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, int>(Comparer, HashFunc, partitionCount: 8);
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            var topicName = $"topic-{i % 20}";
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    grouping.Add(Topic(topicName), i);
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
    public async Task ConcurrentAddRemove_DoesNotThrow()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, int>(Comparer, HashFunc, partitionCount: 8);
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (var i = 0; i < 50; i++)
        {
            var topicName = $"topic-{i % 10}";
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    grouping.Add(Topic(topicName), i);
                    grouping.Remove(Topic(topicName), i);
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
    public async Task ConcurrentRemoveAll_DoesNotThrow()
    {
        var grouping = new Grouping<ReadOnlyMemory<byte>, int>(Comparer, HashFunc, partitionCount: 8);
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Pre-populate
        for (var i = 0; i < 20; i++)
        {
            grouping.Add(Topic($"topic-{i % 5}"), i);
        }

        // Concurrent RemoveAll
        for (var i = 0; i < 20; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    grouping.RemoveAll(value);
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
