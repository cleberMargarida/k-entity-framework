namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class OutboxPatternTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql)
{
    [Fact(Timeout = 60_000)]
    public async Task Given_ProducerWithOutboxPattern_When_PublishingMessage_Then_MessageIsStoredInOutboxAndEventuallyPublished()
    {
        // Arrange
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

    [Fact(Timeout = 60_000)]
    public async Task Given_MultipleTopicsWithDifferentOutboxStrategies_When_ProducingToBoth_Then_EachTopicReceivesOnlyItsOwnMessages()
    {
        // Arrange
        defaultTopic.HasName("outbox-mixed-bg-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        alternativeTopic.HasName("outbox-mixed-immediate-topic");
        alternativeTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseImmediateWithFallback());
        });
        await StartHostAsync();

        // Act
        for (int i = 3000; i <= 3002; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"BackgroundOnly{i}"));
        }
        for (int i = 3100; i <= 3102; i++)
        {
            context.AlternativeMessages.Produce(new AlternativeMessage(i, $"ImmediateFallback{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var defaultResults = await context.Topic<DefaultMessage>().Take(3).ToListAsync(TestContext.Current.CancellationToken);
        var alternativeResults = await context.Topic<AlternativeMessage>().Take(3).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, defaultResults.Count);
        Assert.Equal(3, alternativeResults.Count);

        Assert.All(defaultResults, r => Assert.InRange(r.Id, 3000, 3002));
        Assert.All(alternativeResults, r => Assert.InRange(r.Id, 3100, 3102));

        Assert.True(TopicExist("outbox-mixed-bg-topic"));
        Assert.True(TopicExist("outbox-mixed-immediate-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_OutboxAndForgetOnDifferentTopics_When_ProducingToBoth_Then_EachMiddlewareProcessesOnlyItsOwnMessages()
    {
        // Arrange
        defaultTopic.HasName("outbox-bg-isolation-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        alternativeTopic.HasName("forget-isolation-topic");
        alternativeTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasForget(forget => forget.UseFireForget());
        });
        await StartHostAsync();

        // Act
        for (int i = 3200; i <= 3202; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"OutboxMsg{i}"));
        }
        for (int i = 3300; i <= 3302; i++)
        {
            context.AlternativeMessages.Produce(new AlternativeMessage(i, $"ForgetMsg{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var outboxResults = await context.Topic<DefaultMessage>().Take(3).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, outboxResults.Count);
        Assert.All(outboxResults, r => Assert.InRange(r.Id, 3200, 3202));

        Assert.True(TopicExist("outbox-bg-isolation-topic"));
        Assert.True(TopicExist("forget-isolation-topic"));

        var forgetResults = await context.Topic<AlternativeMessage>().Take(3).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, forgetResults.Count);
        Assert.All(forgetResults, r => Assert.InRange(r.Id, 3300, 3302));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_OutboxWithKeyAndOutboxWithNoKey_When_ProducingToBoth_Then_KeyStrategiesDoNotInterfere()
    {
        // Arrange
        defaultTopic.HasName("outbox-keyed-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        alternativeTopic.HasName("outbox-nokey-topic");
        alternativeTopic.HasProducer(producer =>
        {
            producer.HasNoKey();
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });
        await StartHostAsync();

        // Act
        for (int i = 3400; i <= 3402; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"Keyed{i}"));
        }
        for (int i = 3500; i <= 3502; i++)
        {
            context.AlternativeMessages.Produce(new AlternativeMessage(i, $"NoKey{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var keyedResults = await context.Topic<DefaultMessage>().Take(3).ToListAsync(TestContext.Current.CancellationToken);
        var noKeyResults = await context.Topic<AlternativeMessage>().Take(3).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, keyedResults.Count);
        Assert.Equal(3, noKeyResults.Count);

        Assert.All(keyedResults, r => Assert.InRange(r.Id, 3400, 3402));
        Assert.All(noKeyResults, r => Assert.InRange(r.Id, 3500, 3502));

        Assert.True(TopicExist("outbox-keyed-topic"));
        Assert.True(TopicExist("outbox-nokey-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_OutboxStrategiesWithSequentialSaves_When_AlternatingProduction_Then_MessagesRemainIsolatedAcrossSaves()
    {
        // Arrange
        defaultTopic.HasName("outbox-sequential-bg-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        alternativeTopic.HasName("outbox-sequential-imm-topic");
        alternativeTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseImmediateWithFallback());
        });
        await StartHostAsync();

        // Act - First save: produce to both topics
        context.DefaultMessages.Produce(new DefaultMessage(3600, "BgRound1"));
        context.AlternativeMessages.Produce(new AlternativeMessage(3700, "ImmRound1"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Second save: produce to both topics again
        context.DefaultMessages.Produce(new DefaultMessage(3601, "BgRound2"));
        context.AlternativeMessages.Produce(new AlternativeMessage(3701, "ImmRound2"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var bgResults = await context.Topic<DefaultMessage>().Take(2).ToListAsync(TestContext.Current.CancellationToken);
        var immResults = await context.Topic<AlternativeMessage>().Take(2).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, bgResults.Count);
        Assert.Equal(2, immResults.Count);

        Assert.All(bgResults, r => Assert.InRange(r.Id, 3600, 3601));
        Assert.All(immResults, r => Assert.InRange(r.Id, 3700, 3701));

        Assert.Equal("BgRound1", bgResults.First().Name);
        Assert.Equal("BgRound2", bgResults.Last().Name);
        Assert.Equal("ImmRound1", immResults.First().Name);
        Assert.Equal("ImmRound2", immResults.Last().Name);

        Assert.True(TopicExist("outbox-sequential-bg-topic"));
        Assert.True(TopicExist("outbox-sequential-imm-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_OutboxWithHeadersAndOutboxWithout_When_ProducingToBoth_Then_BothTopicsDeliverCorrectCounts()
    {
        // Arrange
        defaultTopic.HasName("outbox-with-headers-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasHeader(msg => msg.Name);
            producer.HasOutbox(outbox => outbox.UseImmediateWithFallback());
        });

        alternativeTopic.HasName("outbox-no-headers-topic");
        alternativeTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });
        await StartHostAsync();

        // Act
        for (int i = 3800; i <= 3802; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"WithHeader{i}"));
        }
        for (int i = 3900; i <= 3902; i++)
        {
            context.AlternativeMessages.Produce(new AlternativeMessage(i, $"NoHeader{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var headerResults = await context.Topic<DefaultMessage>().Take(3).ToListAsync(TestContext.Current.CancellationToken);
        var noHeaderResults = await context.Topic<AlternativeMessage>().Take(3).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, headerResults.Count);
        Assert.Equal(3, noHeaderResults.Count);

        Assert.All(headerResults, r => Assert.InRange(r.Id, 3800, 3802));
        Assert.All(noHeaderResults, r => Assert.InRange(r.Id, 3900, 3902));

        Assert.True(TopicExist("outbox-with-headers-topic"));
        Assert.True(TopicExist("outbox-no-headers-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_OutboxWorkerWithCustomPollingInterval_When_ProducingToBothTopics_Then_MessagesAreDeliveredWithoutCrossTalk()
    {
        // Arrange - polling interval is a worker-global setting;
        // calling HasOutboxWorker on either topic builder affects the whole worker.
        defaultTopic.HasName("outbox-poll-topic-a");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        alternativeTopic.HasName("outbox-poll-topic-b");
        alternativeTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        modelBuilder.HasOutboxWorker(w => w.WithPollingInterval(TimeSpan.FromMilliseconds(500)));
        await StartHostAsync();

        // Act
        for (int i = 4000; i <= 4004; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"FastPoll{i}"));
        }
        for (int i = 4100; i <= 4104; i++)
        {
            context.AlternativeMessages.Produce(new AlternativeMessage(i, $"SlowPoll{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var fastResults = await context.Topic<DefaultMessage>().Take(5).ToListAsync(TestContext.Current.CancellationToken);
        var slowResults = await context.Topic<AlternativeMessage>().Take(5).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, fastResults.Count);
        Assert.Equal(5, slowResults.Count);

        Assert.All(fastResults, r => Assert.InRange(r.Id, 4000, 4004));
        Assert.All(slowResults, r => Assert.InRange(r.Id, 4100, 4104));

        Assert.True(TopicExist("outbox-poll-topic-a"));
        Assert.True(TopicExist("outbox-poll-topic-b"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_OutboxWorkerWithCustomMaxMessagesPerPoll_When_ProducingBatches_Then_AllMessagesEventuallyDelivered()
    {
        // Arrange - max messages per poll is a worker-global setting;
        // calling HasOutboxWorker on either topic builder affects the whole worker.
        defaultTopic.HasName("outbox-topic-a");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        alternativeTopic.HasName("outbox-topic-b");
        alternativeTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        modelBuilder.HasOutboxWorker(w => w.WithMaxMessagesPerPoll(50));
        await StartHostAsync();

        // Act - produce 6 messages to each topic
        for (int i = 4200; i <= 4205; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"SmallBatch{i}"));
        }
        for (int i = 4300; i <= 4305; i++)
        {
            context.AlternativeMessages.Produce(new AlternativeMessage(i, $"LargeBatch{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - all messages eventually delivered regardless of batch size
        var smallBatchResults = await context.Topic<DefaultMessage>().Take(6).ToListAsync(TestContext.Current.CancellationToken);
        var largeBatchResults = await context.Topic<AlternativeMessage>().Take(6).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(6, smallBatchResults.Count);
        Assert.Equal(6, largeBatchResults.Count);

        Assert.All(smallBatchResults, r => Assert.InRange(r.Id, 4200, 4205));
        Assert.All(largeBatchResults, r => Assert.InRange(r.Id, 4300, 4305));

        Assert.True(TopicExist("outbox-topic-a"));
        Assert.True(TopicExist("outbox-topic-b"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_OutboxWithMixedPollingAndBatchSettings_When_ProducingToBoth_Then_ConfigurationsDoNotInterfere()
    {
        // Arrange - combine polling interval + batch size + different strategies;
        // worker-global settings are configured once via HasOutboxWorker.
        defaultTopic.HasName("outbox-mixed-settings-bg-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseBackgroundOnly());
        });

        alternativeTopic.HasName("outbox-mixed-settings-imm-topic");
        alternativeTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasOutbox(outbox => outbox.UseImmediateWithFallback());
        });

        modelBuilder.HasOutboxWorker(w => w
            .WithPollingInterval(TimeSpan.FromMilliseconds(500))
            .WithMaxMessagesPerPoll(3));

        await StartHostAsync();

        // Act - produce 5 messages to each across two saves
        for (int i = 4400; i <= 4402; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"BgMixed{i}"));
        }
        for (int i = 4500; i <= 4502; i++)
        {
            context.AlternativeMessages.Produce(new AlternativeMessage(i, $"ImmMixed{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DefaultMessages.Produce(new DefaultMessage(4403, "BgMixed4403"));
        context.DefaultMessages.Produce(new DefaultMessage(4404, "BgMixed4404"));
        context.AlternativeMessages.Produce(new AlternativeMessage(4503, "ImmMixed4503"));
        context.AlternativeMessages.Produce(new AlternativeMessage(4504, "ImmMixed4504"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var bgResults = await context.Topic<DefaultMessage>().Take(5).ToListAsync(TestContext.Current.CancellationToken);
        var immResults = await context.Topic<AlternativeMessage>().Take(5).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, bgResults.Count);
        Assert.Equal(5, immResults.Count);

        Assert.All(bgResults, r => Assert.InRange(r.Id, 4400, 4404));
        Assert.All(immResults, r => Assert.InRange(r.Id, 4500, 4504));

        Assert.Equal(5, bgResults.Select(r => r.Id).Distinct().Count());
        Assert.Equal(5, immResults.Select(r => r.Id).Distinct().Count());

        Assert.True(TopicExist("outbox-mixed-settings-bg-topic"));
        Assert.True(TopicExist("outbox-mixed-settings-imm-topic"));
    }
}
