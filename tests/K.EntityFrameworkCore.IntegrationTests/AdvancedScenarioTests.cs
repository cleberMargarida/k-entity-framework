namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class AdvancedScenarioTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    [Fact]
    public async Task Given_ProducingLargeNumberOfMessages_When_SavingInBatches_Then_AllMessagesAreProcessed()
    {
        // Arrange
        defaultTopic.HasName("batch-processing-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        await StartHostAsync();

        // Act
        for (int i = 1; i <= 50; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"BatchMessage{i}"));
        }
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<DefaultMessage>().Take(50).ToListAsync();
        Assert.Equal(50, results.Count);
        Assert.True(TopicExist("batch-processing-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithSequentialMessages_When_ProducingMessagesWithDelay_Then_MessagesMaintainOrder()
    {
        // Arrange
        defaultTopic.HasName("sequential-order-topic");
        defaultTopic.HasSetting(setting => setting.NumPartitions = 1); // Single partition for order
        defaultTopic.HasProducer(producer => producer.HasKey(msg => "order-key"));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(1800, "First"));
        await context.SaveChangesAsync();

        await Task.Delay(100); // Small delay

        context.DefaultMessages.Produce(new DefaultMessage(1801, "Second"));
        await context.SaveChangesAsync();

        context.DefaultMessages.Produce(new DefaultMessage(1802, "Third"));
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<DefaultMessage>().Take(3).ToListAsync();
        Assert.Equal(3, results.Count);
        Assert.Equal("First", results[0].Name);
        Assert.Equal("Second", results[1].Name);
        Assert.Equal("Third", results[2].Name);
        Assert.True(TopicExist("sequential-order-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithLargeMessage_When_ProducingLargeContent_Then_MessageIsProcessedSuccessfully()
    {
        // Arrange
        defaultTopic.HasName("large-message-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        await StartHostAsync();

        var largeContent = new string('X', 10000); // 10KB content

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(1900, largeContent));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1900, result.Id);
        Assert.Equal(largeContent, result.Name);
        Assert.Equal(10000, result.Name.Length);
        Assert.True(TopicExist("large-message-topic"));
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        this.context.Dispose();
    }
}
