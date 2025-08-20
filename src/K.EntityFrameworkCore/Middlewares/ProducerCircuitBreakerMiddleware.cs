namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific circuit breaker middleware that inherits from the base CircuitBreakerMiddleware.
/// Implements the circuit breaker pattern with configurable failure thresholds, timeout periods,
/// and exception filtering specifically for producer scenarios.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerCircuitBreakerMiddleware<T>(ProducerCircuitBreakerMiddlewareSettings<T> settings) : CircuitBreakerMiddleware<T>(settings)
    where T : class
{
}
