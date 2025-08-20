using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Retry;

/// <summary>
/// Producer-specific retry middleware that inherits from the base RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerRetryMiddleware<T>(ProducerRetryMiddlewareSettings<T> settings) : RetryMiddleware<T>(settings)
    where T : class
{
}
