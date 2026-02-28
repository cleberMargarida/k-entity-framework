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
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>(worker =>
        {
            worker.UseExclusiveNode();
        });
        defaultTopic.HasName("exclusive-node-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        // Act
        await StartHostAsync();
        // Allow time for leader election
        await Task.Delay(5000, TestContext.Current.CancellationToken);

        // Assert — the coordination topic should have been auto-created
        Assert.True(TopicExist("__k_outbox_exclusive"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ExclusiveNodeCoordination_When_CustomOptions_Then_CustomTopicIsCreated()
    {
        // Arrange
        const string customTopic = "__k_outbox_exclusive_custom";

        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>(worker =>
        {
            worker.UseExclusiveNode(options =>
            {
                options.TopicName = customTopic;
                options.GroupId = "k-outbox-exclusive-custom";
            });
        });
        defaultTopic.HasName("exclusive-custom-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        // Act
        await StartHostAsync();
        await Task.Delay(5000, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(TopicExist(customTopic));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ExclusiveNodeCoordination_When_SingleNode_Then_ConsumerGroupIsCreated()
    {
        // Arrange
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>(worker =>
        {
            worker.UseExclusiveNode();
        });
        defaultTopic.HasName("exclusive-group-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        // Act
        await StartHostAsync();
        await Task.Delay(5000, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(GroupExist("k-outbox-exclusive"));
    }
}
