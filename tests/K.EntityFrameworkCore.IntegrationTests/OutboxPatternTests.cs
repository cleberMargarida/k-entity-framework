namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class OutboxPatternTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql)
{
    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithOutboxPattern_When_PublishingMessage_Then_MessageIsStoredInOutboxAndEventuallyPublished()
    {
        // Arrange
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>();
        defaultTopic.HasName("outbox-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(42, "OutboxTest"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var result = await context.DefaultMessages.FirstAsync(TestContext.Current.CancellationToken);
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
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseImmediateWithFallback());
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(300, "ImmediateFallback"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var result = await context.DefaultMessages.FirstAsync(TestContext.Current.CancellationToken);
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
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseImmediateWithFallback());
        });
        await StartHostAsync();

        // Act
        for (int i = 1400; i <= 1403; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"ImmediateFallbackBatch{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var results = await context.Topic<DefaultMessage>().Take(4).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(4, results.Count);
        Assert.Equal(1400, results.First().Id);
        Assert.Equal(1403, results.Last().Id);
        Assert.True(TopicExist("immediate-fallback-batch-topic"));
    }
}
