using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer-specific throttle middleware that inherits from the base ThrottleMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerThrottleMiddleware<T>(ConsumerThrottleMiddlewareOptions<T> options) : ThrottleMiddleware<T>(options)
    where T : class
{
}
