using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Consumer;

/// <summary>
/// Tests for <see cref="ConsumerBuilder{T}"/> watermark configuration and validation.
/// </summary>
public class ConsumerBuilderWatermarkTests
{
    private sealed record TestMessage(int Id, string Name);

    [Fact]
    public void WithHighWaterMark_ValidRatio_SetsAnnotation()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act
        var result = builder.WithHighWaterMark(0.90);

        // Assert
        Assert.Same(builder, result);
        var ratio = ((IModel)modelBuilder.Model).GetHighWaterMarkRatio<TestMessage>();
        Assert.Equal(0.90, ratio);
    }

    [Fact]
    public void WithLowWaterMark_ValidRatio_SetsAnnotation()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act
        var result = builder.WithLowWaterMark(0.30);

        // Assert
        Assert.Same(builder, result);
        var ratio = ((IModel)modelBuilder.Model).GetLowWaterMarkRatio<TestMessage>();
        Assert.Equal(0.30, ratio);
    }

    [Theory]
    [InlineData(0.0)]   // at boundary (exclusive)
    [InlineData(-0.1)]  // below zero
    [InlineData(-1.0)]  // negative
    public void WithHighWaterMark_AtOrBelowZero_ThrowsArgumentOutOfRangeException(double ratio)
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithHighWaterMark(ratio));
    }

    [Theory]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void WithHighWaterMark_AboveOne_ThrowsArgumentOutOfRangeException(double ratio)
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithHighWaterMark(ratio));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.0)]
    public void WithLowWaterMark_BelowZero_ThrowsArgumentOutOfRangeException(double ratio)
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithLowWaterMark(ratio));
    }

    [Theory]
    [InlineData(1.0)]   // at boundary (exclusive)
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void WithLowWaterMark_AtOrAboveOne_ThrowsArgumentOutOfRangeException(double ratio)
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithLowWaterMark(ratio));
    }

    [Fact]
    public void WithHighWaterMark_ExactlyOne_IsValid()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act — 1.0 is valid for high water mark (pause only when completely full)
        var result = builder.WithHighWaterMark(1.0);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithLowWaterMark_ExactlyZero_IsValid()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act — 0.0 is valid for low water mark (resume only when completely empty)
        var result = builder.WithLowWaterMark(0.0);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithHighWaterMark_FluentChaining_Works()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var builder = new ConsumerBuilder<TestMessage>(modelBuilder);

        // Act — chain multiple calls
        var result = builder
            .HasMaxBufferedMessages(500)
            .WithHighWaterMark(0.90)
            .WithLowWaterMark(0.20)
            .HasBackpressureMode(ConsumerBackpressureMode.ApplyBackpressure);

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(0.90, ((IModel)modelBuilder.Model).GetHighWaterMarkRatio<TestMessage>());
        Assert.Equal(0.20, ((IModel)modelBuilder.Model).GetLowWaterMarkRatio<TestMessage>());
    }
}
