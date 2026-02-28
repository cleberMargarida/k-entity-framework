using K.EntityFrameworkCore.Diagnostics;
using K.EntityFrameworkCore.Middlewares.Core;
using System.Diagnostics;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Producer middleware that injects W3C Trace Context headers into outgoing messages
/// and records the <c>k_ef.messages.produced</c> counter.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class TracePropagationMiddleware<T>(TracePropagationMiddlewareSettings<T> settings)
    : Middleware<T>(settings)
    where T : class
{
    /// <inheritdoc />
    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        var activity = KafkaDiagnostics.Source.StartActivity("K.EntityFrameworkCore.Produce", ActivityKind.Producer);

        try
        {
            envelope.Headers = TraceContextPropagator.Inject(envelope.Headers, Activity.Current);

            var task = base.InvokeAsync(envelope, cancellationToken);
            if (!task.IsCompletedSuccessfully)
                task.GetAwaiter().GetResult();
            KafkaDiagnostics.MessagesProduced.Add(1);
            return task;
        }
        finally
        {
            activity?.Dispose();
        }
    }
}
