using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer-specific circuit breaker middleware that inherits from the base CircuitBreakerMiddleware.
/// Implements the circuit breaker pattern with configurable failure thresholds, timeout periods,
/// and exception filtering specifically for consumer scenarios.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerCircuitBreakerMiddleware<T>(ConsumerCircuitBreakerMiddlewareOptions<T> options) : CircuitBreakerMiddleware<T>(options)
    where T : class
{
}
