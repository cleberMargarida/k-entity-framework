using K.EntityFrameworkCore.MiddlewareOptions.Consumer;
using Microsoft.Extensions.Logging;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer-specific batch middleware that inherits from the base BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ConsumerBatchMiddleware<T>(ConsumerBatchMiddlewareOptions<T> options) : BatchMiddleware<T>(options)
    where T : class
{
    protected override Task InvokeAsync(ICollection<Envelope<T>> batchToSend, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
