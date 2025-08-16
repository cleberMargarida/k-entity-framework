using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific batch middleware that inherits from the base BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerBatchMiddleware<T>(ProducerBatchMiddlewareOptions<T> options) : BatchMiddleware<T>(options)
    where T : class
{
}
