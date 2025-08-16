using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer-specific fire-forget middleware that inherits from the base FireForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerFireForgetMiddleware<T>(ConsumerFireForgetMiddlewareOptions<T> options) : FireForgetMiddleware<T>(options)
    where T : class
{
}
