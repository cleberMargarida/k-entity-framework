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
    public override ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default) => settings.Strategy switch
    {
        ForgetStrategy.AwaitForget => AwaitForgetProcessing(envelope, cancellationToken),
        ForgetStrategy.FireForget => FireForgetProcessing(envelope, cancellationToken),
        _ => throw new InvalidOperationException($"Unknown forget strategy: {settings.Strategy}"),
    };

    private ValueTask AwaitForgetProcessing(Envelope<T> envelope, CancellationToken cancellationToken)
    {
        return new ValueTask(
            Task.WhenAny(
                base.InvokeAsync(envelope, cancellationToken).AsTask(),
                Task.Delay(settings.Timeout, cancellationToken)));
    }

    private ValueTask FireForgetProcessing(Envelope<T> envelope, CancellationToken cancellationToken)
    {
        _ = base.InvokeAsync(envelope, cancellationToken).AsTask();
        return ValueTask.CompletedTask;
    }
}
