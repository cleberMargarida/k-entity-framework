namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class HeaderFilterTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql)
{
    [Fact]
    public async Task Given_HeaderFilterByTenantId_When_PublishingMessagesForDifferentTenants_Then_OnlyMatchingTenantMessagesAreProcessed()
    {
        // Arrange
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        defaultTopic.HasName("tenant-filtered-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasHeader("tenant-id", msg => msg.Name);
        });
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasHeaderFilter("tenant-id", "tenant-123");
        });

        await StartHostAsync();

        // Act - Produce messages for different tenants
        context.DefaultMessages.Produce(new(3001, "tenant-123"));
        context.DefaultMessages.Produce(new(3002, "tenant-456"));
        context.DefaultMessages.Produce(new(3003, "tenant-123"));
        context.DefaultMessages.Produce(new(3004, "tenant-789"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Only tenant-123 messages should be processed
        var results = await context.DefaultMessages.Take(2).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, results.Count);
        Assert.All(results, message => Assert.Equal("tenant-123", message.Name));
        Assert.Contains(results, msg => msg.Id == 3001);
        Assert.Contains(results, msg => msg.Id == 3003);
    }


    //[Fact(Timeout = 120)]
    [Fact]
    public async Task Given_MultipleHeaderFilters_When_PublishingMessages_Then_OnlyMessagesMatchingAllFiltersAreProcessed()
    {
        // Arrange
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        defaultTopic.HasName("multi-filter-topic");
        defaultTopic.HasProducer(producer =>
        {
            producer.HasKey(msg => msg.Id);
            producer.HasHeader("tenant", msg => msg.Id);
            producer.HasHeader("region", msg => msg.Name);
        });
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasHeaderFilter("tenant", "1");
            consumer.HasHeaderFilter("region", "US");
        });

        await StartHostAsync();

        // Act - Produce messages with different region combinations
        context.DefaultMessages.Produce(new(2, "EU")); // Fail both filters
        context.DefaultMessages.Produce(new(1, "US")); // Pass both filters
        context.DefaultMessages.Produce(new(2, "US")); // Pass region filter only
        context.DefaultMessages.Produce(new(1, "BR")); // Pass tenant filter only
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await context.DefaultMessages.Take(3).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, results.Count);
    }
}
