using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific throttle middleware that inherits from the base ThrottleMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerThrottleMiddleware<T>(ProducerThrottleMiddlewareOptions<T> options) : ThrottleMiddleware<T>(options)
    where T : class
{
}
