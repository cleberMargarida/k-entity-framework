using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Middlewares.Core
{
    public class MiddlewareSettingsTests
    {
        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }

        [Fact]
        public void MiddlewareSettings_DefaultConstructor_ShouldDisableMiddleware()
        {
            // Act
            var settings = new MiddlewareSettings<TestMessage>();

            // Assert
            Assert.False(settings.IsMiddlewareEnabled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MiddlewareSettings_ParameterizedConstructor_ShouldSetEnabled(bool isEnabled)
        {
            // Act
            var settings = new MiddlewareSettings<TestMessage>(isEnabled);

            // Assert
            Assert.Equal(isEnabled, settings.IsMiddlewareEnabled);
        }

        [Fact]
        public void EnableMiddleware_ShouldSetIsMiddlewareEnabledToTrue()
        {
            // Arrange
            var settings = new MiddlewareSettings<TestMessage>(false);

            // Act
            settings.EnableMiddleware();

            // Assert
            Assert.True(settings.IsMiddlewareEnabled);
        }

        [Fact]
        public void DisableMiddleware_ShouldSetIsMiddlewareEnabledToFalse()
        {
            // Arrange
            var settings = new MiddlewareSettings<TestMessage>(true);

            // Act
            settings.DisableMiddleware();

            // Assert
            Assert.False(settings.IsMiddlewareEnabled);
        }

        [Fact]
        public void IsMiddlewareEnabled_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var settings = new MiddlewareSettings<TestMessage>();

            // Act
            settings.IsMiddlewareEnabled = true;

            // Assert
            Assert.True(settings.IsMiddlewareEnabled);

            // Act
            settings.IsMiddlewareEnabled = false;

            // Assert
            Assert.False(settings.IsMiddlewareEnabled);
        }

        [Fact]
        public void EnableMiddleware_WhenAlreadyEnabled_ShouldRemainEnabled()
        {
            // Arrange
            var settings = new MiddlewareSettings<TestMessage>(true);

            // Act
            settings.EnableMiddleware();

            // Assert
            Assert.True(settings.IsMiddlewareEnabled);
        }

        [Fact]
        public void DisableMiddleware_WhenAlreadyDisabled_ShouldRemainDisabled()
        {
            // Arrange
            var settings = new MiddlewareSettings<TestMessage>(false);

            // Act
            settings.DisableMiddleware();

            // Assert
            Assert.False(settings.IsMiddlewareEnabled);
        }

        [Fact]
        public void MiddlewareSettings_ShouldInheritFromSettingsBase()
        {
            // Arrange
            var settings = new MiddlewareSettings<TestMessage>();

            // Assert
            Assert.IsAssignableFrom<SettingsBase<TestMessage>>(settings);
        }

        [Fact]
        public void MiddlewareSettings_ShouldHaveGenericTypeConstraint()
        {
            // Arrange
            var type = typeof(MiddlewareSettings<>);

            // Assert
            var genericParameter = type.GetGenericArguments();
            Assert.Single(genericParameter);
            Assert.True(genericParameter[0].GenericParameterAttributes.HasFlag(System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint));
        }
    }

    public class SettingsBaseTests
    {
        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }

        [Fact]
        public void SettingsBase_ShouldBePublicClass()
        {
            // Arrange
            var type = typeof(SettingsBase<TestMessage>);

            // Assert
            Assert.True(type.IsPublic);
            Assert.True(type.IsClass);
        }

        [Fact]
        public void SettingsBase_ShouldBeGeneric()
        {
            // Arrange
            var type = typeof(SettingsBase<>);

            // Assert
            Assert.True(type.IsGenericTypeDefinition);
            Assert.Single(type.GetGenericArguments());
        }

        [Fact]
        public void SettingsBase_ShouldHaveParameterlessConstructor()
        {
            // Act & Assert - Should not throw
            var instance = new SettingsBase<TestMessage>();
            Assert.NotNull(instance);
        }

        [Fact]
        public void SettingsBase_ShouldAllowInheritance()
        {
            // Arrange
            var type = typeof(SettingsBase<TestMessage>);

            // Assert
            Assert.False(type.IsSealed);
        }
    }

    // Test implementation of Middleware for testing
    internal class TestMiddleware<T> : Middleware<T> where T : class
    {
        public TestMiddleware(MiddlewareSettings<T> settings) : base(settings) { }
        public TestMiddleware() : base() { }

        public bool InvokeAsyncCalled { get; private set; }
        public Envelope<T>? LastEnvelope { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            InvokeAsyncCalled = true;
            LastEnvelope = envelope;
            LastCancellationToken = cancellationToken;
            return base.InvokeAsync(envelope, cancellationToken);
        }
    }

    public class MiddlewareTests
    {
        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }

        [Fact]
        public void Middleware_DefaultConstructor_ShouldCreateWithDefaultSettings()
        {
            // Act
            var middleware = new TestMiddleware<TestMessage>();

            // Assert
            Assert.False(middleware.IsEnabled); // Default settings should disable middleware
        }

        [Fact]
        public void Middleware_ParameterizedConstructor_ShouldUseProvidedSettings()
        {
            // Arrange
            var settings = new MiddlewareSettings<TestMessage>(true);

            // Act
            var middleware = new TestMiddleware<TestMessage>(settings);

            // Assert
            Assert.True(middleware.IsEnabled);
        }

        [Fact]
        public void IsEnabled_ShouldReflectSettingsState()
        {
            // Arrange
            var enabledSettings = new MiddlewareSettings<TestMessage>(true);
            var disabledSettings = new MiddlewareSettings<TestMessage>(false);

            // Act
            var enabledMiddleware = new TestMiddleware<TestMessage>(enabledSettings);
            var disabledMiddleware = new TestMiddleware<TestMessage>(disabledSettings);

            // Assert
            Assert.True(enabledMiddleware.IsEnabled);
            Assert.False(disabledMiddleware.IsEnabled);
        }

        [Fact]
        public void Next_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var middleware1 = new TestMiddleware<TestMessage>();
            var middleware2 = new TestMiddleware<TestMessage>();
            IMiddleware<TestMessage> imiddleware1 = middleware1;

            // Act
            imiddleware1.Next = middleware2;

            // Assert
            Assert.Same(middleware2, imiddleware1.Next);
        }

        [Fact]
        public async Task InvokeAsync_WithoutNext_ShouldCompleteSuccessfully()
        {
            // Arrange
            var middleware = new TestMiddleware<TestMessage>();
            var message = new TestMessage { Value = "test" };
            var envelope = new Envelope<TestMessage>(message);

            // Act & Assert - Should not throw
            await middleware.InvokeAsync(envelope);
            Assert.True(middleware.InvokeAsyncCalled);
        }

        [Fact]
        public async Task InvokeAsync_WithNext_ShouldCallNext()
        {
            // Arrange
            var middleware1 = new TestMiddleware<TestMessage>();
            var middleware2 = new TestMiddleware<TestMessage>();
            var message = new TestMessage { Value = "test" };
            var envelope = new Envelope<TestMessage>(message);

            IMiddleware<TestMessage> imiddleware1 = middleware1;
            imiddleware1.Next = middleware2;

            // Act
            await middleware1.InvokeAsync(envelope);

            // Assert
            Assert.True(middleware1.InvokeAsyncCalled);
            Assert.True(middleware2.InvokeAsyncCalled);
        }

        [Fact]
        public async Task InvokeAsync_ShouldPassEnvelopeToNext()
        {
            // Arrange
            var middleware1 = new TestMiddleware<TestMessage>();
            var middleware2 = new TestMiddleware<TestMessage>();
            var message = new TestMessage { Value = "test" };
            var envelope = new Envelope<TestMessage>(message);

            IMiddleware<TestMessage> imiddleware1 = middleware1;
            imiddleware1.Next = middleware2;

            // Act
            await middleware1.InvokeAsync(envelope);

            // Assert
            Assert.Same(envelope, middleware2.LastEnvelope);
        }

        [Fact]
        public async Task InvokeAsync_ShouldPassCancellationTokenToNext()
        {
            // Arrange
            var middleware1 = new TestMiddleware<TestMessage>();
            var middleware2 = new TestMiddleware<TestMessage>();
            var message = new TestMessage { Value = "test" };
            var envelope = new Envelope<TestMessage>(message);
            var cancellationTokenSource = new CancellationTokenSource();

            IMiddleware<TestMessage> imiddleware1 = middleware1;
            imiddleware1.Next = middleware2;

            // Act
            await middleware1.InvokeAsync(envelope, cancellationTokenSource.Token);

            // Assert
            Assert.Equal(cancellationTokenSource.Token, middleware2.LastCancellationToken);
        }

        [Fact]
        public void Middleware_ShouldImplementIMiddleware()
        {
            // Arrange
            var middleware = new TestMiddleware<TestMessage>();

            // Assert
            Assert.IsAssignableFrom<IMiddleware<TestMessage>>(middleware);
        }

        [Fact]
        public void Middleware_ShouldBeInternal()
        {
            // Arrange
            var type = typeof(Middleware<TestMessage>);

            // Assert
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void Middleware_ShouldBeAbstract()
        {
            // Arrange
            var type = typeof(Middleware<TestMessage>);

            // Assert
            Assert.True(type.IsAbstract);
        }

        [Fact]
        public void Middleware_ShouldHaveGenericTypeConstraint()
        {
            // Arrange
            var type = typeof(Middleware<>);

            // Assert
            var genericParameter = type.GetGenericArguments()[0];
            Assert.True(genericParameter.GenericParameterAttributes.HasFlag(System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint));
        }
    }
}
