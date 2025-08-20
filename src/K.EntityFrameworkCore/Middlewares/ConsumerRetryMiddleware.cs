namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Consumer-specific retry middleware that inherits from the base RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerRetryMiddleware<T>(ConsumerRetryMiddlewareSettings<T> settings) : RetryMiddleware<T>(settings)
    where T : class
{
}
