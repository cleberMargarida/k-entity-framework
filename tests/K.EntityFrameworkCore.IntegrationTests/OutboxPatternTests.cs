using K.EntityFrameworkCore.Middlewares.Outbox;

namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class OutboxPatternTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithOutboxPattern_When_PublishingMessage_Then_MessageIsStoredInOutboxAndEventuallyPublished()
    {
        // Arrange
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>();
        defaultTopic.HasName("outbox-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id.ToString());
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new DefaultMessage(42, "OutboxTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(42, result.Id);
        Assert.Equal("OutboxTest", result.Name);
        Assert.True(TopicExist("outbox-test-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithOutboxImmediateWithFallback_When_PublishingMessage_Then_MessageIsPublishedImmediatelyWithFallback()
    {
        // Arrange
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>();
        defaultTopic.HasName("immediate-fallback-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id.ToString());
            producer.HasOutbox(outbox => outbox.UseImmediateWithFallback());
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new DefaultMessage(300, "ImmediateFallback"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(300, result.Id);
        Assert.Equal("ImmediateFallback", result.Name);
        Assert.True(TopicExist("immediate-fallback-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithOutboxImmediateWithFallbackAndBatch_When_PublishingMessages_Then_MessagesAreHandledCorrectly()
    {
        // Arrange
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>();
        defaultTopic.HasName("immediate-fallback-batch-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id.ToString());
            producer.HasOutbox(outbox => outbox.UseImmediateWithFallback());
        });
        await StartHostAsync();

        // Act
        for (int i = 1400; i <= 1403; i++)
        {
            context.DefaultMessages.Publish(new DefaultMessage(i, $"ImmediateFallbackBatch{i}"));
        }
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<DefaultMessage>().Take(4).ToListAsync();
        Assert.Equal(4, results.Count);
        Assert.Equal(1400, results.First().Id);
        Assert.Equal(1403, results.Last().Id);
        Assert.True(TopicExist("immediate-fallback-batch-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithOutboxBackgroundOnly_And_ConsumerWithInboxDeduplication_When_PublishingMultipleMessages_Then_MessagesAreEventuallyProcessed()
    {
        OutboxPollingWorker<PostgreTestContext>.debugMarker = 840128;
        // Arrange
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>();
        defaultTopic.HasName("outbox-background-batch-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id.ToString());
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasInbox(inbox => inbox.HasDeduplicateProperties(msg => msg.Id));
        });
        await StartHostAsync();

        // Act
        for (int i = 1100; i <= 1105; i++)
        {
            context.DefaultMessages.Publish(new DefaultMessage(i, $"OutboxBatch{i}"));
            await context.SaveChangesAsync();
        }

        // Assert
        var results = await context.Topic<DefaultMessage>().Take(6).ToListAsync();
        Assert.Equal(6, results.Count);
        Assert.Equal(1100, results.First().Id);
        Assert.Equal(1105, results.Last().Id);
        Assert.True(TopicExist("outbox-background-batch-topic"));
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        context.Set<OutboxMessage>().ExecuteDelete();
        context.Dispose();
    }
}
