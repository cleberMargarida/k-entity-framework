using K.EntityFrameworkCore.Middlewares.Outbox;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Extensions;
using Xunit;
using System.Reflection;

namespace K.EntityFrameworkCore.UnitTests.Middlewares.Outbox
{
    public class OutboxMiddlewareSettingsTests
    {
        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }

        [Fact]
        public void OutboxMiddlewareSettings_ShouldHaveSingletonServiceAttribute()
        {
            // Arrange
            var type = typeof(OutboxMiddlewareSettings<TestMessage>);

            // Act
            var attributes = type.GetCustomAttributes<SingletonServiceAttribute>();

            // Assert
            Assert.Single(attributes);
        }

        [Fact]
        public void OutboxMiddlewareSettings_ShouldBePublic()
        {
            // Arrange
            var type = typeof(OutboxMiddlewareSettings<TestMessage>);

            // Assert
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void OutboxMiddlewareSettings_ShouldInheritFromMiddlewareSettings()
        {
            // Arrange
            var settings = new OutboxMiddlewareSettings<TestMessage>();

            // Assert
            Assert.IsAssignableFrom<MiddlewareSettings<TestMessage>>(settings);
        }

        [Fact]
        public void OutboxMiddlewareSettings_DefaultStrategy_ShouldBeBackgroundOnly()
        {
            // Act
            var settings = new OutboxMiddlewareSettings<TestMessage>();

            // Assert
            Assert.Equal(OutboxPublishingStrategy.BackgroundOnly, settings.Strategy);
        }

        [Theory]
        [InlineData(OutboxPublishingStrategy.BackgroundOnly)]
        [InlineData(OutboxPublishingStrategy.ImmediateWithFallback)]
        public void Strategy_SetAndGet_ShouldWorkCorrectly(OutboxPublishingStrategy strategy)
        {
            // Arrange
            var settings = new OutboxMiddlewareSettings<TestMessage>();

            // Act
            settings.Strategy = strategy;

            // Assert
            Assert.Equal(strategy, settings.Strategy);
        }

        [Fact]
        public void OutboxMiddlewareSettings_ShouldHaveGenericTypeConstraint()
        {
            // Arrange
            var type = typeof(OutboxMiddlewareSettings<>);

            // Assert
            var genericParameter = type.GetGenericArguments()[0];
            Assert.True(genericParameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        }

        [Fact]
        public void OutboxMiddlewareSettings_DefaultConstructor_ShouldCreateValidInstance()
        {
            // Act
            var settings = new OutboxMiddlewareSettings<TestMessage>();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal(OutboxPublishingStrategy.BackgroundOnly, settings.Strategy);
        }

        [Fact]
        public void OutboxMiddlewareSettings_ShouldAllowInheritance()
        {
            // Arrange
            var type = typeof(OutboxMiddlewareSettings<TestMessage>);

            // Assert
            Assert.False(type.IsSealed);
        }
    }

    public class OutboxPublishingStrategyTests
    {
        [Fact]
        public void OutboxPublishingStrategy_ShouldBePublicEnum()
        {
            // Arrange
            var type = typeof(OutboxPublishingStrategy);

            // Assert
            Assert.True(type.IsPublic);
            Assert.True(type.IsEnum);
        }

        [Fact]
        public void OutboxPublishingStrategy_ShouldHaveExpectedValues()
        {
            // Act
            var values = Enum.GetValues<OutboxPublishingStrategy>();

            // Assert
            Assert.Contains(OutboxPublishingStrategy.BackgroundOnly, values);
            Assert.Contains(OutboxPublishingStrategy.ImmediateWithFallback, values);
        }

        [Fact]
        public void OutboxPublishingStrategy_ShouldHaveCorrectNames()
        {
            // Act
            var names = Enum.GetNames<OutboxPublishingStrategy>();

            // Assert
            Assert.Contains("BackgroundOnly", names);
            Assert.Contains("ImmediateWithFallback", names);
        }

        [Theory]
        [InlineData(OutboxPublishingStrategy.BackgroundOnly, "BackgroundOnly")]
        [InlineData(OutboxPublishingStrategy.ImmediateWithFallback, "ImmediateWithFallback")]
        public void OutboxPublishingStrategy_ToString_ShouldReturnCorrectName(OutboxPublishingStrategy strategy, string expectedName)
        {
            // Act
            var result = strategy.ToString();

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Fact]
        public void OutboxPublishingStrategy_ShouldHaveCorrectUnderlyingType()
        {
            // Arrange
            var type = typeof(OutboxPublishingStrategy);

            // Assert
            Assert.Equal(typeof(int), Enum.GetUnderlyingType(type));
        }

        [Theory]
        [InlineData(OutboxPublishingStrategy.BackgroundOnly, 0)]
        [InlineData(OutboxPublishingStrategy.ImmediateWithFallback, 1)]
        public void OutboxPublishingStrategy_ShouldHaveExpectedIntegerValues(OutboxPublishingStrategy strategy, int expectedValue)
        {
            // Act
            var value = (int)strategy;

            // Assert
            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void OutboxPublishingStrategy_Parse_ShouldWorkCorrectly()
        {
            // Act
            var backgroundOnly = Enum.Parse<OutboxPublishingStrategy>("BackgroundOnly");
            var immediateWithFallback = Enum.Parse<OutboxPublishingStrategy>("ImmediateWithFallback");

            // Assert
            Assert.Equal(OutboxPublishingStrategy.BackgroundOnly, backgroundOnly);
            Assert.Equal(OutboxPublishingStrategy.ImmediateWithFallback, immediateWithFallback);
        }

        [Fact]
        public void OutboxPublishingStrategy_IsDefined_ShouldWorkCorrectly()
        {
            // Act & Assert
            Assert.True(Enum.IsDefined(OutboxPublishingStrategy.BackgroundOnly));
            Assert.True(Enum.IsDefined(OutboxPublishingStrategy.ImmediateWithFallback));
            Assert.False(Enum.IsDefined((OutboxPublishingStrategy)999));
        }

        [Fact]
        public void OutboxPublishingStrategy_GetValues_ShouldReturnAllValues()
        {
            // Act
            var values = Enum.GetValues<OutboxPublishingStrategy>();

            // Assert
            Assert.Equal(2, values.Length);
            Assert.Contains(OutboxPublishingStrategy.BackgroundOnly, values);
            Assert.Contains(OutboxPublishingStrategy.ImmediateWithFallback, values);
        }

        [Fact]
        public void OutboxPublishingStrategy_Equality_ShouldWorkCorrectly()
        {
            // Act & Assert
            Assert.Equal(OutboxPublishingStrategy.BackgroundOnly, OutboxPublishingStrategy.BackgroundOnly);
            Assert.Equal(OutboxPublishingStrategy.ImmediateWithFallback, OutboxPublishingStrategy.ImmediateWithFallback);
            Assert.NotEqual(OutboxPublishingStrategy.BackgroundOnly, OutboxPublishingStrategy.ImmediateWithFallback);
        }

        [Fact]
        public void OutboxPublishingStrategy_Comparison_ShouldWorkCorrectly()
        {
            // Assert
            Assert.True(OutboxPublishingStrategy.BackgroundOnly < OutboxPublishingStrategy.ImmediateWithFallback);
            Assert.False(OutboxPublishingStrategy.ImmediateWithFallback < OutboxPublishingStrategy.BackgroundOnly);
        }
    }
}
