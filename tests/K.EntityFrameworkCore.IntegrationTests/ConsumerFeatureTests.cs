namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class ConsumerFeatureTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql)
{
    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithInboxDeduplication_When_PublishingDuplicateMessages_Then_DuplicatesAreIgnored()
    {
        // Arrange
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        defaultTopic.HasName("deduplication-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasInbox(inbox =>
            {
                inbox.HasDeduplicateProperties(msg => new { msg.Id });
                inbox.UseDeduplicationTimeWindow(TimeSpan.FromMinutes(5));
            });
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(2000, "DeduplicationTest"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DefaultMessages.Produce(new DefaultMessage(2000, "DeduplicationTest"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var firstMessage = await context.DefaultMessages.FirstAsync();

        /// The second message should not be processed due to deduplication, so we wait 10 seconds before cancel the operation.
        /// In case the second message is processed, the test will fail due no TaskCanceledException being raised.
        await Assert.ThrowsAsync<TaskCanceledException>(() => context.DefaultMessages.FirstAsync().AsTask().WaitAsync(timeout.Token));
    }

    [Fact(Skip = "Good Scenario, but Take will wait indefinitely")]
    public async Task Given_ProducerWithConsumerBackpressureMode_When_PublishingManyMessages_Then_BackpressureIsApplied()
    {
        // Arrange
        defaultTopic.HasName("backpressure-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasMaxBufferedMessages(5);
            consumer.HasBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);
        });
        await StartHostAsync();

        // Act - Produce more messages than buffer size
        for (int i = 2100; i <= 2110; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"BackpressureMessage{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Some messages may be dropped due to backpressure
        var results = await context.Topic<DefaultMessage>().Take(15).ToListAsync();
        Assert.True(results.Count <= 11); // Should not exceed the number published
        Assert.True(TopicExist("backpressure-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithExclusiveConnection_When_PublishingMessages_Then_DedicatedConnectionIsUsed()
    {
        // Arrange
        defaultTopic.HasName("exclusive-connection-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasExclusiveConnection(connection =>
            {
                connection.GroupId = "exclusive-group";
            });
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(2200, "ExclusiveConnectionTest"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(2200, result.Id);
        Assert.Equal("ExclusiveConnectionTest", result.Name);
        Assert.True(TopicExist("exclusive-connection-topic"));
        Assert.True(GroupExist("exclusive-group"));
    }
}
