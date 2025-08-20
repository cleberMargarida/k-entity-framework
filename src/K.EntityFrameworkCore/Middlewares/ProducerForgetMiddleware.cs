namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific forget middleware that inherits from the base ForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerForgetMiddleware<T>(ProducerForgetMiddlewareSettings<T> settings) : ForgetMiddleware<T>(settings)
    where T : class
{
}
