using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific circuit breaker middleware that inherits from the base CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerCircuitBreakerMiddleware<T>(ProducerCircuitBreakerMiddlewareOptions<T> options) : CircuitBreakerMiddleware<T>(options)
    where T : class
{
}
