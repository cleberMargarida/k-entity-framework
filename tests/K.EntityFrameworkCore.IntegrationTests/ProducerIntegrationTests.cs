namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class ProducerIntegrationTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    [Fact]
    public async Task Given_DbContextWithKafka_When_PublishingMessage_Then_MessageIsConsumed()
    {
        // Arrange
        await ApplyModelAndStartHostAsync();

        // Act
        context.Publish(new MessageType(1, default));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Given_PublishingMultipleMessages_When_SavingOnce_Then_MessagesAreConsumed()
    {
        // Arrange
        await ApplyModelAndStartHostAsync();
        context.Publish(new MessageType(1, default));
        context.Publish(new MessageType(2, default));

        // Act
        await context.SaveChangesAsync();

        // Assert
        var result = await context.Topic<MessageType>().Take(2).ToListAsync();
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public async Task Given_PublishingMessageTwice_When_SavingTwice_Then_MessagesAreConsumed()
    {
        // Arrange & Act
        await ApplyModelAndStartHostAsync();

        context.Publish(new MessageType(1, default));
        await context.SaveChangesAsync();

        context.Publish(new MessageType(2, default));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.Topic<MessageType>().Take(2).ToListAsync();
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }


    [Fact]
    public async Task Given_DbContextWithCustomTopicName_When_PublishingMessage_Then_MessageIsSentToCustomTopic()
    {
        // Arrange
        defaultTopic.HasName("custom-name");
        await ApplyModelAndStartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(1, default));
        await context.SaveChangesAsync();

        // Assert
        Assert.True(TopicExist("custom-name"));
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Given_ProducerWithOutboxPattern_When_PublishingMessage_Then_MessageIsStoredInOutboxAndEventuallyPublished()
    {
        // Arrange
        builder.Services.AddOutboxKafkaWorker<PostgreTestContext>();
        defaultTopic.HasName("outbox-test-topic").HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id.ToString());
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });
        await ApplyModelAndStartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(42, "OutboxTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(42, result.Id);
        Assert.Equal("OutboxTest", result.Name);
        Assert.True(TopicExist("outbox-test-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithCustomKey_When_PublishingMessage_Then_MessageIsPublishedWithCorrectKey()
    {
        // Arrange
        defaultTopic.HasName("custom-key-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => $"custom-{msg.Id}"));
        await ApplyModelAndStartHostAsync();

        // Act
        context.Publish(new MessageType(100, "CustomKey"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(100, result.Id);
        Assert.Equal("CustomKey", result.Name);
        Assert.True(TopicExist("custom-key-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithNoKey_When_PublishingMessage_Then_MessageIsDistributedRandomly()
    {
        // Arrange
        defaultTopic.HasSetting(setting => setting.NumPartitions = 2);
        defaultTopic.HasName("random-partition-topic");
        defaultTopic.HasProducer(producer => producer.HasNoKey());
        await ApplyModelAndStartHostAsync();

        // Act
        context.Publish(new MessageType(200, "RandomPartition"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(200, result.Id);
        Assert.Equal("RandomPartition", result.Name);
        Assert.True(TopicExist("random-partition-topic"));
    }

    [Fact]
    public async Task Given_MultipleDifferentTopics_When_PublishingToEach_Then_MessagesAreRoutedCorrectly()
    {
        // Arrange
        defaultTopic.HasName("topic-a");
        alternativeTopic.HasName("topic-b");
        await ApplyModelAndStartHostAsync();

        // Act
        context.Publish(new MessageType(1, "TopicA"));
        await context.SaveChangesAsync();

        context.Publish(new MessageTypeB(2, "TopicB"));
        await context.SaveChangesAsync();

        // Assert
        var message1 = await context.DefaultMessages.FirstAsync();
        var message2 = await context.AlternativeMessages.FirstAsync();
        Assert.True(message1.Id == 1 && message1.Name == "TopicA");
        Assert.True(message2.Id == 2 && message2.Name == "TopicB");
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
        await ApplyModelAndStartHostAsync();

        // Act
        context.Publish(new MessageType(300, "ImmediateFallback"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(300, result.Id);
        Assert.Equal("ImmediateFallback", result.Name);
        Assert.True(TopicExist("immediate-fallback-topic"));
    }

    [Fact(Skip = "Forget not implemented")]
    public async Task Given_ProducerWithForgetMiddleware_When_PublishingMessage_Then_MessageIsPublishedWithFireAndForgetSemantics()
    {
        // Arrange
        defaultTopic.HasName("forget-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id.ToString());
            producer.HasForget(); // Fire and forget semantics
        });
        await ApplyModelAndStartHostAsync();

        // Act
        context.Publish(new MessageType(400, "ForgetSemantic"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(400, result.Id);
        Assert.Equal("ForgetSemantic", result.Name);
        Assert.True(TopicExist("forget-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithJsonSerialization_When_PublishingComplexMessage_Then_MessageIsSerializedCorrectly()
    {
        // Arrange
        defaultTopic.HasName("json-serialization-topic");
        defaultTopic.UseSystemTextJson(options => options.WriteIndented = true);
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        await ApplyModelAndStartHostAsync();

        // Act
        context.Publish(new MessageType(700, "JsonSerialized"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(700, result.Id);
        Assert.Equal("JsonSerialized", result.Name);
        Assert.True(TopicExist("json-serialization-topic"));
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        context.Dispose();
    }
}
