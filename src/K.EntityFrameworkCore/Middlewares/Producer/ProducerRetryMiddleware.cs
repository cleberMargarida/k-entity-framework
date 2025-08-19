using K.EntityFrameworkCore.MiddlewareOptions.Producer;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer-specific retry middleware that inherits from the base RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class ProducerRetryMiddleware<T>(ProducerRetryMiddlewareOptions<T> options) : RetryMiddleware<T>(options)
    where T : class
{
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        return base.InvokeAsync(envelope, cancellationToken);
    }
}
