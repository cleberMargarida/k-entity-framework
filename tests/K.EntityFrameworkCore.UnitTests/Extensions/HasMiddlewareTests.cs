using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Extensions;

/// <summary>
/// Tests for <c>HasMiddleware&lt;TMiddleware&gt;()</c> on <see cref="ProducerBuilder{T}"/> and <see cref="ConsumerBuilder{T}"/>.
/// </summary>
public class HasMiddlewareTests
{
    private sealed record TestMessage(int Id, string Name);

    #region Test middleware types

    private sealed class MiddlewareA : IMiddleware<TestMessage>
    {
        public bool IsEnabled => true;

        IMiddleware<TestMessage>? IMiddleware<TestMessage>.Next { get; set; }

        public ValueTask<TestMessage?> InvokeAsync(Envelope<TestMessage> envelope, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<TestMessage?>(envelope.Message);
    }

    private sealed class MiddlewareB : IMiddleware<TestMessage>
    {
        public bool IsEnabled => true;

        IMiddleware<TestMessage>? IMiddleware<TestMessage>.Next { get; set; }

        public ValueTask<TestMessage?> InvokeAsync(Envelope<TestMessage> envelope, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<TestMessage?>(envelope.Message);
    }

    private sealed class MiddlewareC : IMiddleware<TestMessage>
    {
        public bool IsEnabled => true;

        IMiddleware<TestMessage>? IMiddleware<TestMessage>.Next { get; set; }

        public ValueTask<TestMessage?> InvokeAsync(Envelope<TestMessage> envelope, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<TestMessage?>(envelope.Message);
    }

    #endregion

    #region ProducerBuilder tests

    [Fact]
    public void ProducerBuilder_HasMiddleware_AccumulatesRegistrations()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ProducerBuilder<TestMessage>(modelBuilder);

        // Act
        builder.HasMiddleware<MiddlewareA>();
        builder.HasMiddleware<MiddlewareB>();

        // Assert
        var registrations = ((IModel)modelBuilder.Model).GetUserProducerMiddlewares<TestMessage>();
        Assert.Equal(2, registrations.Count);
    }

    [Fact]
    public void ProducerBuilder_HasMiddleware_PreservesRegistrationOrder()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ProducerBuilder<TestMessage>(modelBuilder);

        // Act
        builder.HasMiddleware<MiddlewareA>();
        builder.HasMiddleware<MiddlewareB>();
        builder.HasMiddleware<MiddlewareC>();

        // Assert
        var registrations = ((IModel)modelBuilder.Model).GetUserProducerMiddlewares<TestMessage>();
        Assert.Equal(3, registrations.Count);
        Assert.Equal(typeof(MiddlewareA), registrations[0].MiddlewareType);
        Assert.Equal(typeof(MiddlewareB), registrations[1].MiddlewareType);
        Assert.Equal(typeof(MiddlewareC), registrations[2].MiddlewareType);
    }

    [Fact]
    public void ProducerBuilder_HasMiddleware_ReturnsSameBuilderForChaining()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ProducerBuilder<TestMessage>(modelBuilder);

        // Act
        var result = builder.HasMiddleware<MiddlewareA>();

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void ProducerBuilder_NoMiddleware_ReturnsEmptyList()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        _ = new ProducerBuilder<TestMessage>(modelBuilder);

        // Act
        var registrations = ((IModel)modelBuilder.Model).GetUserProducerMiddlewares<TestMessage>();

        // Assert
        Assert.Empty(registrations);
    }

    #endregion

    #region ConsumerBuilder tests

    [Fact]
    public void ConsumerBuilder_HasMiddleware_AccumulatesRegistrations()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act
        builder.HasMiddleware<MiddlewareA>();
        builder.HasMiddleware<MiddlewareB>();

        // Assert
        var registrations = ((IModel)modelBuilder.Model).GetUserConsumerMiddlewares<TestMessage>();
        Assert.Equal(2, registrations.Count);
    }

    [Fact]
    public void ConsumerBuilder_HasMiddleware_PreservesRegistrationOrder()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act
        builder.HasMiddleware<MiddlewareA>();
        builder.HasMiddleware<MiddlewareB>();
        builder.HasMiddleware<MiddlewareC>();

        // Assert
        var registrations = ((IModel)modelBuilder.Model).GetUserConsumerMiddlewares<TestMessage>();
        Assert.Equal(3, registrations.Count);
        Assert.Equal(typeof(MiddlewareA), registrations[0].MiddlewareType);
        Assert.Equal(typeof(MiddlewareB), registrations[1].MiddlewareType);
        Assert.Equal(typeof(MiddlewareC), registrations[2].MiddlewareType);
    }

    [Fact]
    public void ConsumerBuilder_HasMiddleware_ReturnsSameBuilderForChaining()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act
        var result = builder.HasMiddleware<MiddlewareA>();

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void ConsumerBuilder_NoMiddleware_ReturnsEmptyList()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        _ = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act
        var registrations = ((IModel)modelBuilder.Model).GetUserConsumerMiddlewares<TestMessage>();

        // Assert
        Assert.Empty(registrations);
    }

    #endregion
}
