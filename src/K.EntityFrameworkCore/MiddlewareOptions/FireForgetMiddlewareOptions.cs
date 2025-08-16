namespace K.EntityFrameworkCore.MiddlewareOptions;

/// <summary>
/// Configuration options for the FireForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class FireForgetMiddlewareOptions<T> : AwaitForgetMiddlewareOptions<T>
    where T : class
{
}
