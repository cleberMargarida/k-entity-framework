using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.MiddlewareOptions.Consumer;

/// <summary>
/// Consumer-specific configuration options for the ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerForgetMiddlewareOptions<T> : ForgetMiddlewareOptions<T>
    where T : class
{
}
