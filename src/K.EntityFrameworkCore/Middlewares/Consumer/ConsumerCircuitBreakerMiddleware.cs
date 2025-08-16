using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer-specific circuit breaker middleware that inherits from the base CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerCircuitBreakerMiddleware<T>(ConsumerCircuitBreakerMiddlewareOptions<T> options) : CircuitBreakerMiddleware<T>(options)
    where T : class
{
}
