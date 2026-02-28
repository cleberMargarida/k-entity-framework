using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Outbox;

public class ExclusiveNodeCoordinationTests
{
    // ----------------------------------------------------------------
    //  ExclusiveNodeOptions defaults
    // ----------------------------------------------------------------

    [Fact]
    public void ExclusiveNodeOptions_HasExpectedDefaults()
    {
        var options = new ExclusiveNodeOptions();

        Assert.Equal("__k_outbox_exclusive", options.TopicName);
        Assert.Equal("k-outbox-exclusive", options.GroupId);
        Assert.Equal(TimeSpan.FromSeconds(3), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromSeconds(30), options.SessionTimeout);
    }

    [Fact]
    public void ExclusiveNodeOptions_CanBeCustomized()
    {
        var options = new ExclusiveNodeOptions
        {
            TopicName = "custom-topic",
            GroupId = "custom-group",
            HeartbeatInterval = TimeSpan.FromSeconds(5),
            SessionTimeout = TimeSpan.FromSeconds(60)
        };

        Assert.Equal("custom-topic", options.TopicName);
        Assert.Equal("custom-group", options.GroupId);
        Assert.Equal(TimeSpan.FromSeconds(5), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromSeconds(60), options.SessionTimeout);
    }

    // ----------------------------------------------------------------
    //  ApplyScope
    // ----------------------------------------------------------------

    [Fact]
    public void ApplyScope_WhenLeader_ReturnsSource()
    {
        var coordination = CreateCoordination();

        // Simulate becoming leader
        coordination.OnPartitionsAssigned(null!, []);

        var source = CreateOutboxMessages(3);
        var result = coordination.ApplyScope(source);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public void ApplyScope_WhenNotLeader_ReturnsEmpty()
    {
        var coordination = CreateCoordination();
        Assert.False(coordination.IsLeader);

        var source = CreateOutboxMessages(3);
        var result = coordination.ApplyScope(source);

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyScope_AfterLeadershipRevoked_ReturnsEmpty()
    {
        var coordination = CreateCoordination();

        coordination.OnPartitionsAssigned(null!, []);
        Assert.True(coordination.IsLeader);

        coordination.OnPartitionsRevoked(null!, []);
        Assert.False(coordination.IsLeader);

        var source = CreateOutboxMessages(2);
        var result = coordination.ApplyScope(source);

        Assert.Empty(result);
    }

    // ----------------------------------------------------------------
    //  Partition callbacks
    // ----------------------------------------------------------------

    [Fact]
    public void OnPartitionsAssigned_SetsLeaderToTrue()
    {
        var coordination = CreateCoordination();
        Assert.False(coordination.IsLeader);

        coordination.OnPartitionsAssigned(null!, [new TopicPartition("t", 0)]);

        Assert.True(coordination.IsLeader);
    }

    [Fact]
    public void OnPartitionsRevoked_SetsLeaderToFalse()
    {
        var coordination = CreateCoordination();

        coordination.OnPartitionsAssigned(null!, []);
        Assert.True(coordination.IsLeader);

        coordination.OnPartitionsRevoked(null!, [new TopicPartitionOffset("t", 0, Offset.Unset)]);

        Assert.False(coordination.IsLeader);
    }

    [Fact]
    public void OnPartitionsAssigned_CalledMultipleTimes_RemainsLeader()
    {
        var coordination = CreateCoordination();

        coordination.OnPartitionsAssigned(null!, []);
        coordination.OnPartitionsAssigned(null!, []);

        Assert.True(coordination.IsLeader);
    }

    // ----------------------------------------------------------------
    //  StopAsync
    // ----------------------------------------------------------------

    [Fact]
    public async Task StopAsync_ClearsLeadership()
    {
        var coordination = CreateCoordination();

        // Simulate becoming leader
        coordination.OnPartitionsAssigned(null!, []);
        Assert.True(coordination.IsLeader);

        await coordination.StopAsync(CancellationToken.None);

        Assert.False(coordination.IsLeader);
    }

    [Fact]
    public async Task StopAsync_WhenNeverStarted_DoesNotThrow()
    {
        var coordination = CreateCoordination();

        // Should complete without error even when StartAsync was never called
        await coordination.StopAsync(CancellationToken.None);

        Assert.False(coordination.IsLeader);
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private static ExclusiveNodeCoordination<TestDbContext> CreateCoordination(ExclusiveNodeOptions? options = null)
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var wrappedOptions = Options.Create(options ?? new ExclusiveNodeOptions());
        var logger = NullLogger<ExclusiveNodeCoordination<TestDbContext>>.Instance;

        return new ExclusiveNodeCoordination<TestDbContext>(serviceProvider, wrappedOptions, logger);
    }

    private static IQueryable<OutboxMessage> CreateOutboxMessages(int count)
    {
        return Enumerable
            .Range(0, count)
            .Select(_ => new OutboxMessage { Id = Guid.NewGuid(), Type = "Test", Topic = "t", Payload = [] })
            .AsQueryable();
    }

    /// <summary>
    /// Minimal DbContext used as a type argument for tests.
    /// </summary>
    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
