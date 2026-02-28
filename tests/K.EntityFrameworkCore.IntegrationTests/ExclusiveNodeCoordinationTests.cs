namespace K.EntityFrameworkCore.IntegrationTests;

/// <summary>
/// Integration tests for <c>ExclusiveNodeCoordination</c> leader election scenarios.
/// These tests require a running Kafka cluster provided by the <see cref="Infrastructure.KafkaFixture"/>.
/// </summary>
[Collection("IntegrationTests")]
public class ExclusiveNodeCoordinationTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql)
{
    [Fact(Timeout = 60_000)]
    public async Task Given_ExclusiveNodeCoordination_When_SingleNode_Then_CoordinationTopicIsCreated()
    {
        // Arrange
        var coordinationTopic = $"__k_outbox_exclusive_{Guid.NewGuid():N}";
        var coordinationGroup = $"k-outbox-exclusive-{Guid.NewGuid():N}";
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>(worker =>
        {
            worker.UseExclusiveNode(options =>
            {
                options.TopicName = coordinationTopic;
                options.GroupId = coordinationGroup;
            });
        });
        defaultTopic.HasName("exclusive-node-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        // Act
        await StartHostAsync();
        await WaitForConditionAsync(() => TopicExist(coordinationTopic));

        // Assert — the coordination topic should have been auto-created
        Assert.True(TopicExist(coordinationTopic));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ExclusiveNodeCoordination_When_CustomOptions_Then_CustomTopicIsCreated()
    {
        // Arrange
        var customTopic = $"__k_outbox_exclusive_custom_{Guid.NewGuid():N}";
        var customGroup = $"k-outbox-exclusive-custom-{Guid.NewGuid():N}";

        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>(worker =>
        {
            worker.UseExclusiveNode(options =>
            {
                options.TopicName = customTopic;
                options.GroupId = customGroup;
            });
        });
        defaultTopic.HasName("exclusive-custom-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        // Act
        await StartHostAsync();
        await WaitForConditionAsync(() => TopicExist(customTopic));

        // Assert
        Assert.True(TopicExist(customTopic));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ExclusiveNodeCoordination_When_SingleNode_Then_ConsumerGroupIsCreated()
    {
        // Arrange
        var coordinationTopic = $"__k_outbox_exclusive_{Guid.NewGuid():N}";
        var coordinationGroup = $"k-outbox-exclusive-{Guid.NewGuid():N}";
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>(worker =>
        {
            worker.UseExclusiveNode(options =>
            {
                options.TopicName = coordinationTopic;
                options.GroupId = coordinationGroup;
            });
        });
        defaultTopic.HasName("exclusive-group-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        // Act
        await StartHostAsync();
        await WaitForConditionAsync(() => GroupExist(coordinationGroup));

        // Assert
        Assert.True(GroupExist(coordinationGroup));
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns <c>true</c> or the timeout elapses.
    /// </summary>
    private static async Task WaitForConditionAsync(Func<bool> condition, int timeoutMs = 15_000, int pollIntervalMs = 200)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
                return;
            await Task.Delay(pollIntervalMs, TestContext.Current.CancellationToken);
        }

        Assert.Fail($"Condition was not met within {timeoutMs}ms.");
    }
}
