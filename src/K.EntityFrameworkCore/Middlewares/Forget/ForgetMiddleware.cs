using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Forget;

/// <summary>
/// Middleware that provides configurable forget strategies for message processing.
/// Supports both await-forget (wait with timeout) and fire-forget (immediate return) modes.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal abstract class ForgetMiddleware<T>(ForgetMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default) => settings.Strategy switch
    {
        ForgetStrategy.AwaitForget => AwaitForgetProcessing(envelope, cancellationToken),
        ForgetStrategy.FireForget => FireForgetProcessing(envelope, cancellationToken),
        _ => throw new InvalidOperationException($"Unknown forget strategy: {settings.Strategy}"),
    };

    private ValueTask<T?> AwaitForgetProcessing(Envelope<T> envelope, CancellationToken cancellationToken)
    {
        return new ValueTask<T?>(
            Task.WhenAny(
                base.InvokeAsync(envelope, cancellationToken).AsTask(),
                Task.Delay(settings.Timeout, cancellationToken)).ContinueWith(static _ => default(T)));
    }

    private ValueTask<T?> FireForgetProcessing(Envelope<T> envelope, CancellationToken cancellationToken)
    {
        _ = base.InvokeAsync(envelope, cancellationToken).AsTask();
        return ValueTask.FromResult(default(T));
    }
}
