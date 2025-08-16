using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific fire-forget middleware that inherits from the base FireForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerFireForgetMiddleware<T>(ProducerFireForgetMiddlewareOptions<T> options) : FireForgetMiddleware<T>(options)
    where T : class
{
}
