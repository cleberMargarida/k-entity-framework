namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Consumer-specific configuration options for the ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerForgetMiddlewareSettings<T> : ForgetMiddlewareSettings<T>
    where T : class
{
}
