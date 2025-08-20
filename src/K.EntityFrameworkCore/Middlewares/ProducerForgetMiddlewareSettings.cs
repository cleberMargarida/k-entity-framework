namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Producer-specific configuration options for the ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerForgetMiddlewareSettings<T> : ForgetMiddlewareSettings<T>
    where T : class
{
}
