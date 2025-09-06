using System.Text.Json;

namespace K.EntityFrameworkCore.IntegrationTests;

[Collection("IntegrationTests")]
public class SerializationTests(KafkaFixture kafka, PostgreSqlFixture postgreSql) : IntegrationTest(kafka, postgreSql), IDisposable
{
    [Fact]
    public async Task Given_ProducerWithJsonSerialization_When_ProducingComplexMessage_Then_MessageIsSerializedCorrectly()
    {
        // Arrange
        defaultTopic.HasName("json-serialization-topic");
        defaultTopic.UseSystemTextJson(options => options.WriteIndented = true);
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(700, "JsonSerialized"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(700, result.Id);
        Assert.Equal("JsonSerialized", result.Name);
        Assert.True(TopicExist("json-serialization-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithJsonSerializationAndCustomOptions_When_ProducingMessage_Then_MessageUsesCustomJsonOptions()
    {
        // Arrange
        defaultTopic.HasName("custom-json-topic");
        defaultTopic.UseSystemTextJson(options =>
        {
            options.WriteIndented = false;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(1200, "CustomJsonOptions"));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.Equal(1200, result.Id);
        Assert.Equal("CustomJsonOptions", result.Name);
        Assert.True(TopicExist("custom-json-topic"));
    }

    [Fact]
    public async Task Given_ProducerWithMixedSerializationStrategies_When_ProducingToMultipleTopics_Then_EachUsesCorrectSerialization()
    {
        // Arrange
        defaultTopic.HasName("json-camel-case-topic")
                   .UseSystemTextJson(options =>
                   {
                       options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                       options.WriteIndented = true;
                   })
                   .HasProducer(producer => producer.HasKey(msg => msg.Id));

        alternativeTopic.HasName("json-snake-case-topic")
                       .UseSystemTextJson(options =>
                       {
                           options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                           options.WriteIndented = false;
                       })
                       .HasProducer(producer => producer.HasKey(msg => msg.Id));

        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new DefaultMessage(2600, "CamelCaseJson"));
        context.AlternativeMessages.Produce(new AlternativeMessage(2601, "SnakeCaseJson"));
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
    public async Task Given_ProducerWithPolymorphicMessage_When_ProducingDerivedType_Then_SerializationIncludesTypeDiscriminator()
    {
        // Arrange
        defaultTopic.HasName("polymorphic-default-topic");
        defaultTopic.UseSystemTextJson(options =>
        {
            options.WriteIndented = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        defaultTopic.HasProducer(producer => producer.HasKey(msg => msg.Id));
        await StartHostAsync();

        // Act
        context.DefaultMessages.Produce(new ExtendedMessage(4000, "PolymorphicMessage", "Technology", 499.99m));
        await context.SaveChangesAsync();

        // Assert
        var result = await context.DefaultMessages.FirstAsync();
        Assert.IsType<ExtendedMessage>(result);
        var extendedResult = (ExtendedMessage)result;
        Assert.Equal(4000, extendedResult.Id);
        Assert.Equal("PolymorphicMessage", extendedResult.Name);
        Assert.Equal("Technology", extendedResult.Category);
        Assert.Equal(499.99m, extendedResult.Value);
        Assert.True(TopicExist("polymorphic-default-topic"));
    }

    public void Dispose()
    {
        DeleteKafkaTopics();
        context.Dispose();
    }
}
