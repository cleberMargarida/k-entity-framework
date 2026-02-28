using System.Collections.Concurrent;
using System.Diagnostics;

namespace K.EntityFrameworkCore.IntegrationTests.Diagnostics;

[Collection("IntegrationTests")]
public class EndToEndTraceTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    private readonly ActivityListener activityListener = new()
    {
        ShouldListenTo = source => source.Name == "K.EntityFrameworkCore",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    };

    private readonly ConcurrentBag<Activity> capturedActivities = [];

    [Fact]
    public async Task Given_ProduceAndConsume_When_TracingEnabled_Then_TraceIdPropagated()
    {
        // Arrange
        activityListener.ActivityStarted = activity => capturedActivities.Add(activity);
        ActivitySource.AddActivityListener(activityListener);

        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(1, "traced"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await context.DefaultMessages.FirstAsync(TestContext.Current.CancellationToken);

        // Assert - message was delivered
        Assert.Equal(1, result.Id);

        // Assert - tracing activities were created
        var produceActivities = capturedActivities.Where(a => a.DisplayName == "K.EntityFrameworkCore.Produce").ToList();
        var consumeActivities = capturedActivities.Where(a => a.DisplayName == "K.EntityFrameworkCore.Consume").ToList();

        Assert.NotEmpty(produceActivities);
        Assert.NotEmpty(consumeActivities);

        // Assert - TraceId is propagated from producer to consumer
        var produceTraceId = produceActivities.First().TraceId.ToString();
        var consumeTraceId = consumeActivities.First().TraceId.ToString();

        Assert.Equal(produceTraceId, consumeTraceId);
    }

    [Fact]
    public async Task Given_MultipleMessages_When_Produced_Then_EachGetsUniqueSpanId()
    {
        // Arrange
        activityListener.ActivityStarted = activity => capturedActivities.Add(activity);
        ActivitySource.AddActivityListener(activityListener);

        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(1, "first"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DefaultMessages.Produce(new DefaultMessage(2, "second"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await context.Topic<DefaultMessage>().Take(2).ToListAsync(TestContext.Current.CancellationToken);

        // Assert - each produce creates a separate span
        var produceActivities = capturedActivities.Where(a => a.DisplayName == "K.EntityFrameworkCore.Produce").ToList();
        Assert.True(produceActivities.Count >= 2);

        var spanIds = produceActivities.Select(a => a.SpanId.ToString()).Distinct().ToList();
        Assert.True(spanIds.Count >= 2, "Each produce should have a unique SpanId");
    }

    public new void Dispose()
    {
        activityListener.Dispose();
        base.Dispose();
    }
}
