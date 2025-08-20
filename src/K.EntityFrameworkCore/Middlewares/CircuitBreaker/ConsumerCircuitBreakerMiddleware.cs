using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.CircuitBreaker;

/// <summary>
/// Consumer-specific circuit breaker middleware that inherits from the base CircuitBreakerMiddleware.
/// Implements the circuit breaker pattern with configurable failure thresholds, timeout periods,
/// and exception filtering specifically for consumer scenarios.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerCircuitBreakerMiddleware<T>(ConsumerCircuitBreakerMiddlewareSettings<T> settings) : CircuitBreakerMiddleware<T>(settings)
    where T : class
{
}
