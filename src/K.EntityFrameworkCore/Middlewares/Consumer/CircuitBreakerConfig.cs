using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Internal record implementing <see cref="ICircuitBreakerConfig"/> with default values.
/// </summary>
internal sealed record CircuitBreakerConfig : ICircuitBreakerConfig
{
    /// <inheritdoc />
    public int TripThreshold { get; init; } = 5;

    /// <inheritdoc />
    public int WindowSize { get; init; } = 10;

    /// <inheritdoc />
    public int ActiveThreshold { get; init; } = 1;

    /// <inheritdoc />
    public TimeSpan ResetInterval { get; init; } = TimeSpan.FromSeconds(30);
}
