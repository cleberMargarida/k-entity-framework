using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.Extensions.Logging;

namespace K.EntityFrameworkCore.Middlewares.Batch;

/// <summary>
/// Consumer-specific batch middleware that inherits from the base BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerBatchMiddleware<T>(ConsumerBatchMiddlewareSettings<T> settings) : BatchMiddleware<T>(settings)
    where T : class
{
}
