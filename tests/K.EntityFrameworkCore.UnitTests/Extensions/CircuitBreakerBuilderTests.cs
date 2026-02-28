using K.EntityFrameworkCore.Extensions;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Consumer;

/// <summary>
/// Tests for <see cref="CircuitBreakerBuilder"/>.
/// </summary>
public class CircuitBreakerBuilderTests
{
    [Fact]
    public void DefaultValues_MatchSpec()
    {
        var builder = new CircuitBreakerBuilder();
        var config = builder.Build();

        Assert.Equal(5, config.TripThreshold);
        Assert.Equal(10, config.WindowSize);
        Assert.Equal(1, config.ActiveThreshold);
        Assert.Equal(TimeSpan.FromSeconds(30), config.ResetInterval);
    }

    [Fact]
    public void TripAfter_SetsCustomTripThreshold()
    {
        var builder = new CircuitBreakerBuilder();
        var config = builder.TripAfter(3).Build();

        Assert.Equal(3, config.TripThreshold);
    }

    [Fact]
    public void HasWindowSize_SetsCustomWindowSize()
    {
        var builder = new CircuitBreakerBuilder();
        var config = builder.HasWindowSize(20).Build();

        Assert.Equal(20, config.WindowSize);
    }

    [Fact]
    public void HasResetInterval_SetsCustomResetInterval()
    {
        var builder = new CircuitBreakerBuilder();
        var config = builder.HasResetInterval(TimeSpan.FromMinutes(2)).Build();

        Assert.Equal(TimeSpan.FromMinutes(2), config.ResetInterval);
    }

    [Fact]
    public void HasActiveThreshold_SetsCustomActiveThreshold()
    {
        var builder = new CircuitBreakerBuilder();
        var config = builder.HasActiveThreshold(5).Build();

        Assert.Equal(5, config.ActiveThreshold);
    }

    [Fact]
    public void FluentChaining_AllCustomValues()
    {
        var config = new CircuitBreakerBuilder()
            .TripAfter(10)
            .HasWindowSize(50)
            .HasResetInterval(TimeSpan.FromSeconds(60))
            .HasActiveThreshold(3)
            .Build();

        Assert.Equal(10, config.TripThreshold);
        Assert.Equal(50, config.WindowSize);
        Assert.Equal(TimeSpan.FromSeconds(60), config.ResetInterval);
        Assert.Equal(3, config.ActiveThreshold);
    }

    [Fact]
    public void FluentChaining_ReturnsSameBuilder()
    {
        var builder = new CircuitBreakerBuilder();

        var result = builder
            .TripAfter(3)
            .HasWindowSize(5)
            .HasResetInterval(TimeSpan.FromSeconds(10))
            .HasActiveThreshold(2);

        Assert.Same(builder, result);
    }

    [Fact]
    public void Build_ReturnsICircuitBreakerConfig()
    {
        var builder = new CircuitBreakerBuilder();
        ICircuitBreakerConfig config = builder.Build();

        Assert.NotNull(config);
    }
}
