namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class BasicMessageProducingTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    [Fact]
    public async Task Given_DbContextWithKafka_When_ProducingMessage_Then_MessageIsConsumed()
    {
        // Arrange
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(1, default));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Given_ProducingMultipleMessages_When_SavingOnce_Then_MessagesAreConsumed()
    {
        // Arrange
        await StartHostAsync();
        context.DefaultMessages.Produce(new DefaultMessage(1, default));
        context.DefaultMessages.Produce(new DefaultMessage(2, default));

        // Act
        await context.SaveChangesAsync();

        // Assert
        var result = await context.Topic<DefaultMessage>().Take(2).ToListAsync();
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public async Task Given_ProducingMessageTwice_When_SavingTwice_Then_MessagesAreConsumed()
    {
        // Arrange & Act
        await StartHostAsync();

        context.DefaultMessages.Produce(new DefaultMessage(1, default));
        await context.SaveChangesAsync();

        context.DefaultMessages.Produce(new DefaultMessage(2, default));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.Topic<DefaultMessage>().Take(2).ToListAsync();
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public async Task Given_MultipleDifferentTopics_When_ProducingToEach_Then_MessagesAreRoutedCorrectly()
    {
        // Arrange
        defaultTopic.HasName("topic-a");
        alternativeTopic.HasName("topic-b");
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(1, "TopicA"));
        await context.SaveChangesAsync();

        context.AlternativeMessages.Produce(new AlternativeMessage(2, "TopicB"));
        await context.SaveChangesAsync();

        // Assert
        var message1 = await context.DefaultMessages.FirstAsync();
        var message2 = await context.AlternativeMessages.FirstAsync();
        Assert.True(message1.Id == 1 && message1.Name == "TopicA");
        Assert.True(message2.Id == 2 && message2.Name == "TopicB");
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        context.Dispose();
    }
}
