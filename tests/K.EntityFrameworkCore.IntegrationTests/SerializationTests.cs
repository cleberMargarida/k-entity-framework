using System.Text.Json;
using System.Text.Json.Serialization;

namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class SerializationTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    [Fact]
    public async Task Given_ProducerWithJsonSerialization_When_PublishingComplexMessage_Then_MessageIsSerializedCorrectly()
    {
        // Arrange
        defaultTopic.HasName("json-serialization-topic");
        defaultTopic.UseSystemTextJson(options => options.WriteIndented = true);
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id.ToString()));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Publish(new DefaultMessage(700, "JsonSerialized"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(700, result.Id);
        Assert.Equal("JsonSerialized", result.Name);
        Assert.True(TopicExist("json-serialization-topic"));
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
        context.DefaultMessages.Publish(new DefaultMessage(1200, "CustomJsonOptions"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1200, result.Id);
        Assert.Equal("CustomJsonOptions", result.Name);
        Assert.True(TopicExist("custom-json-topic"));
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
        context.DefaultMessages.Publish(new DefaultMessage(2600, "CamelCaseJson"));
        context.AlternativeMessages.Publish(new AlternativeMessage(2601, "SnakeCaseJson"));
        await context.SaveChangesAsync();

        // Assert
        var result1 = await context.DefaultMessages.FirstAsync();
        var result2 = await context.AlternativeMessages.FirstAsync();
        Assert.Equal(2600, result1.Id);
        Assert.Equal(2601, result2.Id);
        Assert.True(TopicExist("json-camel-case-topic"));
        Assert.True(TopicExist("json-snake-case-topic"));
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        context.Dispose();
    }
}
