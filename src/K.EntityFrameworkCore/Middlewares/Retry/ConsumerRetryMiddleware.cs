using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Retry;

/// <summary>
/// Consumer-specific retry middleware that inherits from the base RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerRetryMiddleware<T>(ConsumerRetryMiddlewareSettings<T> settings) : RetryMiddleware<T>(settings)
    where T : class
{
}
