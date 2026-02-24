namespace K.EntityFrameworkCore.IntegrationTests;

/// <summary>
/// End-to-end Debezium CDC tests exercising the full outbox flow:
/// <c>SaveChanges (outbox write)  Debezium WAL  Kafka  Topic&lt;T&gt; consumer</c>.
/// Infrastructure (PostgreSQL, Kafka, Kafka Connect) is managed by
/// <see cref="DebeziumFixture"/> via Testcontainers.
/// </summary>
[Collection("DebeziumTests")]
public class DebeziumOutboxTests(DebeziumFixture debezium) : DebeziumIntegrationTest(debezium)
{
    [Fact(Timeout = 120_000)]
    public async Task Given_OutboxProducer_WithDebeziumCdc_When_TransactionIsCommitted_Then_EventIsConsumedFromKafka()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        orderEventTopic.HasName("order-placed-topic");
        orderEventTopic.HasProducer(producer =>
        {
            producer.HasHeader(x => x.PlacedAt);
            producer.HasOutbox();
        });
        await StartHostAsync();

        context.Orders.Add(new DebeziumOrder { Id = orderId, Description = "Debezium CDC integration test" });

        // Act
        context.OrderEvents.Produce(new DebeziumOrderPlaced { Id = orderId, PlacedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var received = await context.OrderEvents.FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(orderId, received.Id);
    }
}
