using System.Text.Json;
using System.Text.Json.Serialization;

namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class ProducerIntegrationTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    [Fact]
    public async Task Given_DbContextWithKafka_When_PublishingMessage_Then_MessageIsConsumed()
    {
        // Arrange
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(1, default));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Given_PublishingMultipleMessages_When_SavingOnce_Then_MessagesAreConsumed()
    {
        // Arrange
        await StartHostAsync();
        context.DefaultMessages.Publish(new MessageType(1, default));
        context.DefaultMessages.Publish(new MessageType(2, default));

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
        await StartHostAsync();

        context.DefaultMessages.Publish(new MessageType(1, default));
        await context.SaveChangesAsync();

        context.DefaultMessages.Publish(new MessageType(2, default));
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
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(1, default));
        await context.SaveChangesAsync();

        // Assert
        Assert.True(TopicExist("custom-name"));
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1, result.Id);
    }

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
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(100, "CustomKey"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(100, result.Id);
        Assert.Equal("CustomKey", result.Name);
        Assert.True(TopicExist("custom-key-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithNoKey_When_PublishingMessage_Then_MessageIsDistributedRandomly()
    {
        // Arrange
        defaultTopic.HasSetting(setting => setting.NumPartitions = 2);
        defaultTopic.HasName("random-partition-topic");
        defaultTopic.HasProducer(producer => producer.HasNoKey());
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(200, "RandomPartition"));
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
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(1, "TopicA"));
        await context.SaveChangesAsync();

        context.AlternativeMessages.Publish(new MessageTypeB(2, "TopicB"));
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
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(300, "ImmediateFallback"));
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
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(400, "ForgetSemantic"));
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
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(700, "JsonSerialized"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(700, result.Id);
        Assert.Equal("JsonSerialized", result.Name);
        Assert.True(TopicExist("json-serialization-topic"));
    }

    [Fact]
    public async Task Given_PublishingLargeNumberOfMessages_When_SavingInBatches_Then_AllMessagesAreProcessed()
    {
        // Arrange
        defaultTopic.HasName("batch-processing-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        await StartHostAsync();

        // Act
        for (int i = 1; i <= 50; i++)
        {
            context.DefaultMessages.Publish(new MessageType(i, $"BatchMessage{i}"));
        }
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<MessageType>().Take(50).ToListAsync();
        Assert.Equal(50, results.Count);
        Assert.Equal(1, results.First().Id);
        Assert.Equal(50, results.Last().Id);
        Assert.True(TopicExist("batch-processing-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithMultiplePartitions_When_PublishingWithSameKey_Then_MessagesGoToSamePartition()
    {
        // Arrange
        defaultTopic.HasName("multiple-partitions-topic");
        defaultTopic.HasSetting(setting => setting.NumPartitions = 4);
        defaultTopic.HasProducer(producer => producer.HasKey(msg => "fixed-key"));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(800, "SameKeyMessage1"));
        context.DefaultMessages.Publish(new MessageType(801, "SameKeyMessage2"));
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<MessageType>().Take(2).ToListAsync();
        Assert.Equal(2, results.Count);
        Assert.Equal(800, results[0].Id);
        Assert.Equal(801, results[1].Id);
        Assert.True(TopicExist("multiple-partitions-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithComplexKey_When_PublishingMessage_Then_MessageIsPublishedWithComplexKey()
    {
        // Arrange
        defaultTopic.HasName("complex-key-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => $"{msg.Id}_{msg.Name}_{DateTime.UtcNow:yyyyMM}"));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(900, "ComplexKeyTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(900, result.Id);
        Assert.Equal("ComplexKeyTest", result.Name);
        Assert.True(TopicExist("complex-key-topic"));
    }

    [Fact]
    public async Task Given_MixedProducerConfigurations_When_PublishingToMultipleTopics_Then_EachTopicUsesCorrectConfiguration()
    {
        // Arrange
        defaultTopic.HasName("topic-with-key")
                   .HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        
        alternativeTopic.HasName("topic-without-key")
                       .HasProducer(producer => producer.HasNoKey());
        
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(1000, "WithKey"));
        context.AlternativeMessages.Publish(new MessageTypeB(2000, "WithoutKey"));
        await context.SaveChangesAsync();

        // Assert
        var result1 = await context.DefaultMessages.FirstAsync();
        var result2 = await context.AlternativeMessages.FirstAsync();
        Assert.Equal(1000, result1.Id);
        Assert.Equal(2000, result2.Id);
        Assert.True(TopicExist("topic-with-key"));
        Assert.True(TopicExist("topic-without-key"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithOutboxBackgroundOnly_And_ConsumerWithInboxDeduplication_When_PublishingMultipleMessages_Then_MessagesAreEventuallyProcessed()
    {
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
            context.DefaultMessages.Publish(new MessageType(i, $"OutboxBatch{i}"));
            await context.SaveChangesAsync();
        }

        // Assert
        var results = await context.Topic<MessageType>().Take(6).ToListAsync();
        Assert.Equal(6, results.Count);
        Assert.Equal(1100, results.First().Id);
        Assert.Equal(1105, results.Last().Id);
        Assert.True(TopicExist("outbox-background-batch-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithJsonSerializationAndCustomOptions_When_PublishingMessage_Then_MessageUsesCustomJsonOptions()
    {
        // Arrange
        defaultTopic.HasName("custom-json-topic");
        defaultTopic.UseSystemTextJson(options => 
        {
            options.WriteIndented = false;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(1200, "CustomJsonOptions"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1200, result.Id);
        Assert.Equal("CustomJsonOptions", result.Name);
        Assert.True(TopicExist("custom-json-topic"));
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
            context.DefaultMessages.Publish(new MessageType(i, $"ImmediateFallbackBatch{i}"));
        }
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<MessageType>().Take(4).ToListAsync();
        Assert.Equal(4, results.Count);
        Assert.Equal(1400, results.First().Id);
        Assert.Equal(1403, results.Last().Id);
        Assert.True(TopicExist("immediate-fallback-batch-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithSpecialCharactersTopic_When_PublishingMessage_Then_TopicIsCreatedWithSpecialCharacters()
    {
        // Arrange
        defaultTopic.HasName("special-chars_topic.test-123");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(1600, "SpecialCharsTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1600, result.Id);
        Assert.Equal("SpecialCharsTest", result.Name);
        Assert.True(TopicExist("special-chars_topic.test-123"));
    }

    [Fact]
    public async Task Given_ProducerWithSequentialMessages_When_PublishingMessagesWithDelay_Then_MessagesMaintainOrder()
    {
        // Arrange
        defaultTopic.HasName("sequential-order-topic");
        defaultTopic.HasSetting(setting => setting.NumPartitions = 1); // Single partition for order
        defaultTopic.HasProducer(producer => producer.HasKey(msg => "order-key"));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(1800, "First"));
        await context.SaveChangesAsync();
        
        await Task.Delay(100); // Small delay
        
        context.DefaultMessages.Publish(new MessageType(1801, "Second"));
        await context.SaveChangesAsync();
        
        context.DefaultMessages.Publish(new MessageType(1802, "Third"));
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<MessageType>().Take(3).ToListAsync();
        Assert.Equal(3, results.Count);
        Assert.Equal("First", results[0].Name);
        Assert.Equal("Second", results[1].Name);
        Assert.Equal("Third", results[2].Name);
        Assert.True(TopicExist("sequential-order-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithLargeMessage_When_PublishingLargeContent_Then_MessageIsProcessedSuccessfully()
    {
        // Arrange
        defaultTopic.HasName("large-message-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        await StartHostAsync();

        var largeContent = new string('X', 10000); // 10KB content

        // Act
        context.DefaultMessages.Publish(new MessageType(1900, largeContent));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1900, result.Id);
        Assert.Equal(largeContent, result.Name);
        Assert.Equal(10000, result.Name.Length);
        Assert.True(TopicExist("large-message-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithInboxDeduplication_When_PublishingDuplicateMessages_Then_DuplicatesAreIgnored()
    {
        // Arrange
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        defaultTopic.HasName("deduplication-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
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
        context.DefaultMessages.Publish(new MessageType(2000, "DeduplicationTest"));
        await context.SaveChangesAsync();
        
        context.DefaultMessages.Publish(new MessageType(2000, "DeduplicationTest"));
        await context.SaveChangesAsync();

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
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasMaxBufferedMessages(5);
            consumer.HasBackpressureMode(ConsumerBackpressureMode.DropOldestMessage);
        });
        await StartHostAsync();

        // Act - Publish more messages than buffer size
        for (int i = 2100; i <= 2110; i++)
        {
            context.DefaultMessages.Publish(new MessageType(i, $"BackpressureMessage{i}"));
        }
        await context.SaveChangesAsync();

        // Assert - Some messages may be dropped due to backpressure
        var results = await context.Topic<MessageType>().Take(15).ToListAsync();
        Assert.True(results.Count <= 11); // Should not exceed the number published
        Assert.True(TopicExist("backpressure-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithExclusiveConnection_When_PublishingMessages_Then_DedicatedConnectionIsUsed()
    {
        // Arrange
        defaultTopic.HasName("exclusive-connection-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasExclusiveConnection();
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(2200, "ExclusiveConnectionTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(2200, result.Id);
        Assert.Equal("ExclusiveConnectionTest", result.Name);
        Assert.True(TopicExist("exclusive-connection-topic"));
    }

    [Fact(Skip = "Forget is not implemented yet")]
    public async Task Given_ProducerWithForgetAwaitStrategy_When_PublishingMessage_Then_AwaitForgetSemantics()
    {
        // Arrange
        defaultTopic.HasName("await-forget-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id.ToString());
            producer.HasForget(forget => forget.UseAwaitForget(TimeSpan.FromSeconds(10)));
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(2300, "AwaitForgetTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(2300, result.Id);
        Assert.Equal("AwaitForgetTest", result.Name);
        Assert.True(TopicExist("await-forget-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithFireForgetStrategy_When_PublishingMessage_Then_FireForgetSemantics()
    {
        // Arrange
        defaultTopic.HasName("fire-forget-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id.ToString());
            producer.HasForget(forget => forget.UseFireForget());
        });
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(2400, "FireForgetTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(2400, result.Id);
        Assert.Equal("FireForgetTest", result.Name);
        Assert.True(TopicExist("fire-forget-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithComplexTopicSettings_When_PublishingMessage_Then_CustomSettingsApplied()
    {
        // Arrange
        defaultTopic.HasName("complex-settings-topic");
        defaultTopic.HasSetting(setting =>
        {
            setting.NumPartitions = 6;
            setting.ReplicationFactor = 1;
        });
        defaultTopic.HasProducer(producer => producer.HasKey(msg => $"partition-{msg.Id % 3}"));
        await StartHostAsync();

        // Act
        for (int i = 2500; i <= 2505; i++)
        {
            context.DefaultMessages.Publish(new MessageType(i, $"ComplexSettings{i}"));
        }
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<MessageType>().Take(6).ToListAsync();
        Assert.Equal(6, results.Count);
        Assert.True(TopicExist("complex-settings-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithMixedSerializationStrategies_When_PublishingToMultipleTopics_Then_EachUsesCorrectSerialization()
    {
        // Arrange
        defaultTopic.HasName("json-camel-case-topic")
                   .UseSystemTextJson(options => 
                   {
                       options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                       options.WriteIndented = true;
                   })
                   .HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));

        alternativeTopic.HasName("json-snake-case-topic")
                       .UseSystemTextJson(options => 
                       {
                           options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                           options.WriteIndented = false;
                       })
                       .HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));

        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new MessageType(2600, "CamelCaseJson"));
        context.AlternativeMessages.Publish(new MessageTypeB(2601, "SnakeCaseJson"));
        await context.SaveChangesAsync();

        // Assert
        var result1 = await context.DefaultMessages.FirstAsync();
        var result2 = await context.AlternativeMessages.FirstAsync();
        Assert.Equal(2600, result1.Id);
        Assert.Equal(2601, result2.Id);
        Assert.True(TopicExist("json-camel-case-topic"));
        Assert.True(TopicExist("json-snake-case-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithComplexKeyStrategy_When_PublishingWithCompositeKeys_Then_PartitioningWorksCorrectly()
    {
        // Arrange
        defaultTopic.HasName("composite-key-topic");
        defaultTopic.HasSetting(setting => setting.NumPartitions = 4);
        defaultTopic.HasProducer(producer => producer.HasKey(msg => 
            $"{msg.Id % 2}_{(msg.Name != null ? msg.Name.GetHashCode() : 0)}_{DateTime.UtcNow:yyyyMMdd}"));
        await StartHostAsync();

        // Act
        for (int i = 2800; i <= 2810; i++)
        {
            context.DefaultMessages.Publish(new MessageType(i, $"CompositeKey{i}"));
        }
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<MessageType>().Take(11).ToListAsync();
        Assert.Equal(11, results.Count);
        Assert.True(results.All(r => r.Id >= 2800 && r.Id <= 2810));
        Assert.True(TopicExist("composite-key-topic"));
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        context.Dispose();
    }
}
