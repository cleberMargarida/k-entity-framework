using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer-specific retry middleware that inherits from the base RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerRetryMiddleware<T>(ConsumerRetryMiddlewareOptions<T> options) : RetryMiddleware<T>(options)
    where T : class
{
}
