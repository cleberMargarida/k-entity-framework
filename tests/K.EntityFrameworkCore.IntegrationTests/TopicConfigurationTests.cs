using System.Text.Json;
using System.Text.Json.Serialization;

namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class TopicConfigurationTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    [Fact]
    public async Task Given_DbContextWithCustomTopicName_When_ProducingMessage_Then_MessageIsSentToCustomTopic()
    {
        // Arrange
        defaultTopic.HasName("custom-name");
        await StartHostAsync();

        // Act
    context.DefaultMessages.Produce(new DefaultMessage(1, default));
        await context.SaveChangesAsync();

        // Assert
        Assert.True(TopicExist("custom-name"));
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Given_ProducerWithSpecialCharactersTopic_When_ProducingMessage_Then_TopicIsCreatedWithSpecialCharacters()
    {
        // Arrange
        defaultTopic.HasName("special-chars_topic.test-123");
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        await StartHostAsync();

        // Act
    context.DefaultMessages.Produce(new DefaultMessage(1600, "SpecialCharsTest"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1600, result.Id);
        Assert.Equal("SpecialCharsTest", result.Name);
        Assert.True(TopicExist("special-chars_topic.test-123"));
    }

    [Fact]
    public async Task Given_ProducerWithComplexTopicSettings_When_ProducingMessage_Then_CustomSettingsApplied()
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
            context.DefaultMessages.Produce(new DefaultMessage(i, $"ComplexSettings{i}"));
        }
        await context.SaveChangesAsync();

        // Assert
        var results = await context.Topic<DefaultMessage>().Take(6).ToListAsync();
        Assert.Equal(6, results.Count);
        Assert.True(TopicExist("complex-settings-topic"));
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        context.Dispose();
    }
}
