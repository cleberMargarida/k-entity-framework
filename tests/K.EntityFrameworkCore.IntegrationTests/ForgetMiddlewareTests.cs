namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class ForgetMiddlewareTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
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
    context.DefaultMessages.Produce(new DefaultMessage(400, "ForgetSemantic"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(400, result.Id);
        Assert.Equal("ForgetSemantic", result.Name);
        Assert.True(TopicExist("forget-topic"));
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
    context.DefaultMessages.Produce(new DefaultMessage(2300, "AwaitForgetTest"));
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
    context.DefaultMessages.Produce(new DefaultMessage(2400, "FireForgetTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(2400, result.Id);
        Assert.Equal("FireForgetTest", result.Name);
        Assert.True(TopicExist("fire-forget-topic"));
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        context.Dispose();
    }
}
