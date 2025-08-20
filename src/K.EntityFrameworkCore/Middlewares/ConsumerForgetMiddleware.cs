namespace K.EntityFrameworkCore.Middlewares;

/// <summary>
/// Consumer-specific forget middleware that inherits from the base ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerForgetMiddleware<T>(ConsumerForgetMiddlewareSettings<T> settings) : ForgetMiddleware<T>(settings)
    where T : class
{
}
