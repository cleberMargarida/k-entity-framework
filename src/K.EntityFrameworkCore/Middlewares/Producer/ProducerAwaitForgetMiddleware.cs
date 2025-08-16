using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific await-forget middleware that inherits from the base AwaitForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerAwaitForgetMiddleware<T>(ProducerAwaitForgetMiddlewareOptions<T> options) : AwaitForgetMiddleware<T>(options)
    where T : class
{
}
