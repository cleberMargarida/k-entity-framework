using K.EntityFrameworkCore.Diagnostics;
using K.EntityFrameworkCore.Middlewares.Core;
using System.Diagnostics;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Consumer middleware that extracts W3C Trace Context from incoming message headers,
/// restores the parent <see cref="Activity"/> for distributed tracing continuity,
/// and records the <c>k_ef.messages.consumed</c> counter.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class TraceExtractionMiddleware<T>(TraceExtractionMiddlewareSettings<T> settings)
    : Middleware<T>(settings)
    where T : class
{
    /// <inheritdoc />
    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        var parentContext = TraceContextPropagator.Extract(envelope.Headers);

        using var activity = parentContext.HasValue
            ? KafkaDiagnostics.Source.StartActivity("K.EntityFrameworkCore.Consume", ActivityKind.Consumer, parentContext.Value)
            : KafkaDiagnostics.Source.StartActivity("K.EntityFrameworkCore.Consume", ActivityKind.Consumer);

        KafkaDiagnostics.MessagesConsumed.Add(1);

        return base.InvokeAsync(envelope, cancellationToken);
    }
}
