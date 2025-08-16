using K.EntityFrameworkCore.MiddlewareOptions.Consumer;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer-specific await-forget middleware that inherits from the base AwaitForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerAwaitForgetMiddleware<T>(ConsumerAwaitForgetMiddlewareOptions<T> options) : AwaitForgetMiddleware<T>(options)
    where T : class
{
}
