using K.EntityFrameworkCore.Middlewares.Serialization;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Extensions;
using System.Text.Json;
using Xunit;
using System.Reflection;

namespace K.EntityFrameworkCore.UnitTests.Middlewares.Serialization
{
    public class SerializationMiddlewareSettingsTests
    {
        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
            public int Number { get; set; }
        }

        [Fact]
        public void SerializationMiddlewareSettings_ShouldHaveSingletonServiceAttribute()
        {
            // Arrange
            var type = typeof(SerializationMiddlewareSettings<TestMessage>);

            // Act
            var attributes = type.GetCustomAttributes<SingletonServiceAttribute>();

            // Assert
            Assert.Single(attributes);
        }

        [Fact]
        public void SerializationMiddlewareSettings_ShouldBeInternal()
        {
            // Arrange
            var type = typeof(SerializationMiddlewareSettings<TestMessage>);

            // Assert
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void SerializationMiddlewareSettings_ShouldInheritFromMiddlewareSettings()
        {
            // Arrange
            var settings = new SerializationMiddlewareSettings<TestMessage>();

            // Assert
            Assert.IsAssignableFrom<MiddlewareSettings<TestMessage>>(settings);
        }

        [Fact]
        public void SerializationMiddlewareSettings_ShouldBeEnabledByDefault()
        {
            // Act
            var settings = new SerializationMiddlewareSettings<TestMessage>();

            // Assert
            Assert.True(settings.IsMiddlewareEnabled);
        }

        [Fact]
        public void SerializationMiddlewareSettings_ShouldHaveDefaultSerializer()
        {
            // Act
            var settings = new SerializationMiddlewareSettings<TestMessage>();

            // Assert
            Assert.NotNull(settings.Serializer);
            Assert.IsAssignableFrom<IMessageSerializer<TestMessage>>(settings.Serializer);
        }

        [Fact]
        public void SerializationMiddlewareSettings_ShouldHaveDefaultDeserializer()
        {
            // Act
            var settings = new SerializationMiddlewareSettings<TestMessage>();

            // Assert
            Assert.NotNull(settings.Deserializer);
            Assert.IsAssignableFrom<IMessageDeserializer<TestMessage>>(settings.Deserializer);
        }

        [Fact]
        public void SerializationMiddlewareSettings_DefaultSerializerAndDeserializer_ShouldBeSameInstance()
        {
            // Act
            var settings = new SerializationMiddlewareSettings<TestMessage>();

            // Assert
            Assert.Same(settings.Serializer, settings.Deserializer);
        }

        [Fact]
        public void SerializationMiddlewareSettings_Serializer_ShouldBeSettable()
        {
            // Arrange
            var settings = new SerializationMiddlewareSettings<TestMessage>();
            var customSerializer = new CustomTestSerializer();

            // Act
            settings.Serializer = customSerializer;

            // Assert
            Assert.Same(customSerializer, settings.Serializer);
        }

        [Fact]
        public void SerializationMiddlewareSettings_Deserializer_ShouldBeSettable()
        {
            // Arrange
            var settings = new SerializationMiddlewareSettings<TestMessage>();
            var customDeserializer = new CustomTestDeserializer();

            // Act
            settings.Deserializer = customDeserializer;

            // Assert
            Assert.Same(customDeserializer, settings.Deserializer);
        }

        [Fact]
        public void SerializationMiddlewareSettings_ShouldHaveGenericTypeConstraint()
        {
            // Arrange
            var type = typeof(SerializationMiddlewareSettings<>);

            // Assert
            var genericParameter = type.GetGenericArguments()[0];
            Assert.True(genericParameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        }

        private class CustomTestSerializer : IMessageSerializer<TestMessage>
        {
            public byte[] Serialize(in TestMessage message)
            {
                return System.Text.Encoding.UTF8.GetBytes(message.Value);
            }
        }

        private class CustomTestDeserializer : IMessageDeserializer<TestMessage>
        {
            public TestMessage? Deserialize(byte[] data)
            {
                return new TestMessage { Value = System.Text.Encoding.UTF8.GetString(data) };
            }
        }
    }

    public class SystemTextJsonSerializerTests
    {
        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
            public int Number { get; set; }
            public DateTime Timestamp { get; set; }
        }

        [Fact]
        public void SystemTextJsonSerializer_ShouldBeInternal()
        {
            // Arrange
            var type = typeof(SystemTextJsonSerializer<TestMessage>);

            // Assert
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void SystemTextJsonSerializer_ShouldImplementIMessageSerializer()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();

            // Assert
            Assert.IsAssignableFrom<IMessageSerializer<TestMessage>>(serializer);
        }

        [Fact]
        public void SystemTextJsonSerializer_ShouldImplementIMessageDeserializer()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();

            // Assert
            Assert.IsAssignableFrom<IMessageDeserializer<TestMessage>>(serializer);
        }

        [Fact]
        public void SystemTextJsonSerializer_ShouldHaveJsonSerializerOptions()
        {
            // Act
            var serializer = new SystemTextJsonSerializer<TestMessage>();

            // Assert
            Assert.NotNull(serializer.Options);
            Assert.IsType<JsonSerializerOptions>(serializer.Options);
        }

        [Fact]
        public void SystemTextJsonSerializer_Options_ShouldBeReadOnly()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();

            // Act
            var options1 = serializer.Options;
            var options2 = serializer.Options;

            // Assert
            Assert.Same(options1, options2);
        }

        [Fact]
        public void Serialize_WithValidMessage_ShouldReturnByteArray()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();
            var message = new TestMessage 
            { 
                Value = "test", 
                Number = 42,
                Timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var result = serializer.Serialize(message);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Serialize_WithEmptyMessage_ShouldReturnByteArray()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();
            var message = new TestMessage();

            // Act
            var result = serializer.Serialize(message);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Deserialize_WithValidBytes_ShouldReturnMessage()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();
            var originalMessage = new TestMessage 
            { 
                Value = "test", 
                Number = 42,
                Timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };
            var bytes = serializer.Serialize(originalMessage);

            // Act
            var result = serializer.Deserialize(bytes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(originalMessage.Value, result.Value);
            Assert.Equal(originalMessage.Number, result.Number);
            Assert.Equal(originalMessage.Timestamp, result.Timestamp);
        }

        [Fact]
        public void Deserialize_WithEmptyBytes_ShouldThrowException()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();
            var emptyBytes = Array.Empty<byte>();

            // Act & Assert
            Assert.Throws<JsonException>(() => serializer.Deserialize(emptyBytes));
        }

        [Fact]
        public void Deserialize_WithInvalidJson_ShouldThrowException()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();
            var invalidBytes = System.Text.Encoding.UTF8.GetBytes("invalid json");

            // Act & Assert
            Assert.Throws<JsonException>(() => serializer.Deserialize(invalidBytes));
        }

        [Fact]
        public void SerializeDeserialize_RoundTrip_ShouldPreserveData()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();
            var originalMessage = new TestMessage 
            { 
                Value = "Hello, World!", 
                Number = 12345,
                Timestamp = DateTime.Now
            };

            // Act
            var bytes = serializer.Serialize(originalMessage);
            var deserializedMessage = serializer.Deserialize(bytes);

            // Assert
            Assert.NotNull(deserializedMessage);
            Assert.Equal(originalMessage.Value, deserializedMessage.Value);
            Assert.Equal(originalMessage.Number, deserializedMessage.Number);
            Assert.Equal(originalMessage.Timestamp, deserializedMessage.Timestamp);
        }

        [Fact]
        public void SystemTextJsonSerializer_ShouldHaveGenericTypeConstraint()
        {
            // Arrange
            var type = typeof(SystemTextJsonSerializer<>);

            // Assert
            var genericParameter = type.GetGenericArguments()[0];
            Assert.True(genericParameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        }

        [Fact]
        public void Serialize_WithNullProperties_ShouldSerializeCorrectly()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();
            var message = new TestMessage 
            { 
                Value = null!, 
                Number = 0,
                Timestamp = default
            };

            // Act
            var result = serializer.Serialize(message);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            
            // Verify it can be deserialized back
            var deserialized = serializer.Deserialize(result);
            Assert.NotNull(deserialized);
        }

        [Fact]
        public void Deserialize_WithNullInJson_ShouldHandleCorrectly()
        {
            // Arrange
            var serializer = new SystemTextJsonSerializer<TestMessage>();
            var jsonWithNull = """{"Value":null,"Number":42,"Timestamp":"2023-01-01T12:00:00Z"}""";
            var bytes = System.Text.Encoding.UTF8.GetBytes(jsonWithNull);

            // Act
            var result = serializer.Deserialize(bytes);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Value);
            Assert.Equal(42, result.Number);
        }
    }
}
