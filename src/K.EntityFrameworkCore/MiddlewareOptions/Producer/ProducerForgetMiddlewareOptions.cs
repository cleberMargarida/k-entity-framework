using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.MiddlewareOptions.Producer;

/// <summary>
/// Producer-specific configuration options for the ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerForgetMiddlewareOptions<T> : ForgetMiddlewareOptions<T>
    where T : class
{
}
