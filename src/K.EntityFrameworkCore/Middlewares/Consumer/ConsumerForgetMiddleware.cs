using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer-specific forget middleware that inherits from the base ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerForgetMiddleware<T>(ConsumerForgetMiddlewareOptions<T> options) : ForgetMiddleware<T>(options)
    where T : class
{
}
