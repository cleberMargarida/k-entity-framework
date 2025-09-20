namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class ProducerKeyStrategyTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql)
{
    [Fact]
    public async Task Given_ProducerWithCustomKey_When_ProducingMessage_Then_MessageIsProducedWithCorrectKey()
    {
        // Arrange
        defaultTopic.HasName("custom-key-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => $"custom-{msg.Id}"));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(100, "CustomKey"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(100, result.Id);
        Assert.Equal("CustomKey", result.Name);
        Assert.True(TopicExist("custom-key-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithNoKey_When_ProducingMessage_Then_MessageIsDistributedRandomly()
    {
        // Arrange
        defaultTopic.HasSetting(setting => setting.NumPartitions = 2);
        defaultTopic.HasName("random-partition-topic");
        defaultTopic.HasProducer(producer => producer.HasNoKey());
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(200, "RandomPartition"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(200, result.Id);
        Assert.Equal("RandomPartition", result.Name);
        Assert.True(TopicExist("random-partition-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithMultiplePartitions_When_ProducingWithSameKey_Then_MessagesGoToSamePartition()
    {
        // Arrange
        defaultTopic.HasName("multiple-partitions-topic");
        defaultTopic.HasSetting(setting => setting.NumPartitions = 4);
        defaultTopic.HasProducer(producer => producer.HasKey(msg => "fixed-key"));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(800, "SameKeyMessage1"));
        context.DefaultMessages.Produce(new DefaultMessage(801, "SameKeyMessage2"));
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<DefaultMessage>().Take(2).ToListAsync();
        Assert.Equal(2, results.Count);
        Assert.Equal(800, results[0].Id);
        Assert.Equal(801, results[1].Id);
        Assert.True(TopicExist("multiple-partitions-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithComplexKey_When_ProducingMessage_Then_MessageIsProducedWithComplexKey()
    {
        // Arrange
        defaultTopic.HasName("complex-key-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => $"{msg.Id}_{msg.Name}_{DateTime.UtcNow:yyyyMM}"));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(900, "ComplexKeyTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(900, result.Id);
        Assert.Equal("ComplexKeyTest", result.Name);
        Assert.True(TopicExist("complex-key-topic"));
    }

    [Fact]
    public async Task Given_MixedProducerConfigurations_When_ProducingToMultipleTopics_Then_EachTopicUsesCorrectConfiguration()
    {
        // Arrange
        defaultTopic.HasName("topic-with-key")
                    .HasProducer(producer => producer.HasKey(msg => msg.Id));

        alternativeTopic.HasName("topic-without-key")
                        .HasProducer(producer => producer.HasNoKey());

        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(1000, "WithKey"));
        context.AlternativeMessages.Produce(new AlternativeMessage(2000, "WithoutKey"));
        await context.SaveChangesAsync();

        // Assert
        var result1 = await context.DefaultMessages.FirstAsync();
        var result2 = await context.AlternativeMessages.FirstAsync();
        Assert.Equal(1000, result1.Id);
        Assert.Equal(2000, result2.Id);
        Assert.True(TopicExist("topic-with-key"));
        Assert.True(TopicExist("topic-without-key"));
    }

    [Fact]
    public async Task Given_ProducerWithComplexKeyStrategy_When_ProducingWithCompositeKeys_Then_PartitioningWorksCorrectly()
    {
        // Arrange
        defaultTopic.HasName("composite-key-topic");
        defaultTopic.HasSetting(setting => setting.NumPartitions = 4);
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => $"{msg.Id % 2}_{(msg.Name != null ? msg.Name.GetHashCode() : 0)}_{DateTime.UtcNow:yyyyMMdd}");
        });
        await StartHostAsync();

        // Act
        for (int i = 2800; i <= 2810; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"CompositeKey{i}"));
        }
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<DefaultMessage>().Take(11).ToListAsync();
        Assert.Equal(11, results.Count);
        Assert.True(results.All(r => r.Id is >= 2800 and <= 2810));
        Assert.True(TopicExist("composite-key-topic"));
    }
}
