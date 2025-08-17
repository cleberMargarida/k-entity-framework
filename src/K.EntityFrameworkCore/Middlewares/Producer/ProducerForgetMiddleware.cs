using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific forget middleware that inherits from the base ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerForgetMiddleware<T>(ProducerForgetMiddlewareOptions<T> options) : ForgetMiddleware<T>(options)
    where T : class
{
}
