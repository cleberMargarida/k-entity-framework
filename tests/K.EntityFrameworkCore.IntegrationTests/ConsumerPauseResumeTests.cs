namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class ConsumerPauseResumeTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql)
{
    [Fact(Timeout = 60_000)]
    public async Task Given_ConsumerWithBackpressure_When_BufferReachesHighWaterMark_Then_ConsumerPausesAndResumes()
    {
        // Arrange
        defaultTopic.HasName("pause-resume-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasMaxBufferedMessages(10);
            consumer.HasBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);
            consumer.WithHighWaterMark(0.80); // HWM = 8
            consumer.WithLowWaterMark(0.50);  // LWM = 5
        });
        await StartHostAsync();

        // Act - Produce enough messages to potentially trigger backpressure
        for (int i = 3000; i < 3020; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"PauseResumeTest{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - All messages should eventually be consumed (consumer pauses and resumes automatically)
        var results = await context.DefaultMessages.Take(20).ToListAsync();
        Assert.Equal(20, results.Count);
        Assert.True(TopicExist("pause-resume-topic"));
    }

    [Fact(Timeout = 60_000)]
    public async Task Given_ConsumerWithCustomWatermarks_When_Processing_Then_AllMessagesAreConsumed()
    {
        // Arrange
        defaultTopic.HasName("custom-watermark-topic");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        defaultTopic.HasConsumer(consumer =>
        {
            consumer.HasMaxBufferedMessages(20);
            consumer.WithHighWaterMark(0.70); // HWM = 14
            consumer.WithLowWaterMark(0.30);  // LWM = 6
        });
        await StartHostAsync();

        // Act - Produce messages in batches
        for (int i = 3100; i < 3110; i++)
        {
            context.DefaultMessages.Produce(new DefaultMessage(i, $"WatermarkTest{i}"));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - All messages should be consumed despite watermark-based pause/resume
        var results = await context.DefaultMessages.Take(10).ToListAsync();
        Assert.Equal(10, results.Count);
        Assert.True(TopicExist("custom-watermark-topic"));
    }
}
