using Confluent.Kafka;
using Xunit.Abstractions;

namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class ProducerIntegrationTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTestBase(kafka, postgreSql)
{
    [Fact]
    public async Task Given_DbContextWithKafka_When_PublishingMessage_Then_MessageIsConsumed()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        // Act
        Context.Publish(new MessageType(1, default));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().FirstAsync();
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Given_PublishingMultipleMessages_When_SavingOnce_Then_MessagesAreConsumed()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();
        Context.Publish(new MessageType(1, default));
        Context.Publish(new MessageType(2, default));

        // Act
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().Take(2).ToListAsync();
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public async Task Given_PublishingMessageTwice_When_SavingTwice_Then_MessagesAreConsumed()
    {
        // Arrange & Act
        Builder.AddSingleTopicDbContextWithKafka();

        Context.Publish(new MessageType(1, default));
        await Context.SaveChangesAsync();

        Context.Publish(new MessageType(2, default));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().Take(2).ToListAsync();
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }


    [Fact]
    public async Task Given_DbContextWithCustomTopicName_When_PublishingMessage_Then_MessageIsSentToCustomTopic()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        OnModelCreating(model =>
        {
            model.Topic<MessageType>(topic =>
            {
                topic.HasName("custom-name");
            });
        });

        // Act
        Context.Publish(new MessageType(1, default));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().FirstAsync();
        Assert.Equal(1, result.Id);
        Assert.True(TopicExist("custom-name"));
    }

    [Fact]
    public async Task Given_ProducerWithOutboxPattern_When_PublishingMessage_Then_MessageIsStoredInOutboxAndEventuallyPublished()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        OnModelCreating(model =>
        {
            model.Topic<MessageType>(topic =>
            {
                topic.HasName("outbox-test-topic");
                topic.HasProducer(producer =>
                {
                    producer.HasKey(msg => msg.Id.ToString());
                    producer.HasOutbox(outbox =>
                    {
                        outbox.UseBackgroundOnly();
                    });
                });
            });
        });

        // Act
        Context.Publish(new MessageType(42, "OutboxTest"));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().FirstAsync();
        Assert.Equal(42, result.Id);
        Assert.Equal("OutboxTest", result.Name);
        Assert.True(TopicExist("outbox-test-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithCustomKey_When_PublishingMessage_Then_MessageIsPublishedWithCorrectKey()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        OnModelCreating(model =>
        {
            model.Topic<MessageType>(topic =>
            {
                topic.HasName("custom-key-topic");
                topic.HasProducer(producer =>
                {
                    producer.HasKey(msg => $"custom-{msg.Id}");
                });
            });
        });

        // Act
        Context.Publish(new MessageType(100, "CustomKey"));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().FirstAsync();
        Assert.Equal(100, result.Id);
        Assert.Equal("CustomKey", result.Name);
        Assert.True(TopicExist("custom-key-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithNoKey_When_PublishingMessage_Then_MessageIsDistributedRandomly()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        OnModelCreating(model =>
        {
            model.Topic<MessageType>(topic =>
            {
                topic.HasSetting(setting => setting.NumPartitions = 2);
                topic.HasName("random-partition-topic");
                topic.HasProducer(producer =>
                {
                    producer.HasNoKey(); // Random distribution
                });
            });
        });

        // Act
        Context.Publish(new MessageType(200, "RandomPartition"));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().FirstAsync();
        Assert.Equal(200, result.Id);
        Assert.Equal("RandomPartition", result.Name);
        Assert.True(TopicExist("random-partition-topic"));
    }

    [Fact(Timeout = 2000)]
    public async Task Given_MultipleDifferentTopics_When_PublishingToEach_Then_MessagesAreRoutedCorrectly()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        OnModelCreating(model =>
        {
            model.Topic<MessageType>(topic =>
            {
                topic.HasName("topic-a");
            });

            model.Topic<MessageTypeB>(topic =>
            {
                topic.HasName("topic-b");
            });
        });

        // Act
        Context.Publish(new MessageType(1, "TopicA"));
        await Context.SaveChangesAsync();

        Context.Publish(new MessageTypeB(2, "TopicB"));
        await Context.SaveChangesAsync();

        // Assert
        Assert.True(TopicExist("topic-a"));
        Assert.True(TopicExist("topic-b"));
        var message1 = await Context.Topic<MessageType>().FirstAsync();
        var message2 = await Context.Topic<MessageTypeB>().FirstAsync();
        Assert.True(message1.Id == 1 && message1.Name == "TopicA");
        Assert.True(message1.Id == 2 && message1.Name == "TopicB");
    }

    [Fact]
    public async Task Given_ProducerWithOutboxImmediateWithFallback_When_PublishingMessage_Then_MessageIsPublishedImmediatelyWithFallback()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        OnModelCreating(model =>
        {
            model.Topic<MessageType>(topic =>
            {
                topic.HasName("immediate-fallback-topic");
                topic.HasProducer(producer =>
                {
                    producer.HasKey(msg => msg.Id.ToString());
                    producer.HasOutbox(outbox =>
                    {
                        outbox.UseImmediateWithFallback();
                    });
                });
            });
        });

        // Act
        Context.Publish(new MessageType(300, "ImmediateFallback"));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().FirstAsync();
        Assert.Equal(300, result.Id);
        Assert.Equal("ImmediateFallback", result.Name);
        Assert.True(TopicExist("immediate-fallback-topic"));
    }

    [Fact(Skip = "Forget not implemented")]
    public async Task Given_ProducerWithForgetMiddleware_When_PublishingMessage_Then_MessageIsPublishedWithFireAndForgetSemantics()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        OnModelCreating(model =>
        {
            model.Topic<MessageType>(topic =>
            {
                topic.HasName("forget-topic");
                topic.HasProducer(producer =>
                {
                    producer.HasKey(msg => msg.Id.ToString());
                    producer.HasForget(); // Fire and forget semantics
                });
            });
        });

        // Act
        Context.Publish(new MessageType(400, "ForgetSemantic"));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().FirstAsync();
        Assert.Equal(400, result.Id);
        Assert.Equal("ForgetSemantic", result.Name);
        Assert.True(TopicExist("forget-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithJsonSerialization_When_PublishingComplexMessage_Then_MessageIsSerializedCorrectly()
    {
        // Arrange
        Builder.AddSingleTopicDbContextWithKafka();

        OnModelCreating(model =>
        {
            model.Topic<MessageType>(topic =>
            {
                topic.HasName("json-serialization-topic");
                topic.UseSystemTextJson(options =>
                {
                    options.WriteIndented = true;
                });
                topic.HasProducer(producer =>
                {
                    producer.HasKey(msg => msg.Id.ToString());
                });
            });
        });

        // Act
        Context.Publish(new MessageType(700, "JsonSerialized"));
        await Context.SaveChangesAsync();

        // Assert
        var result = await Context.Topic<MessageType>().FirstAsync();
        Assert.Equal(700, result.Id);
        Assert.Equal("JsonSerialized", result.Name);
        Assert.True(TopicExist("json-serialization-topic"));
    }
}
    