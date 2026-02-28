using System.Collections.Immutable;
using System.Diagnostics;

namespace K.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Provides W3C Trace Context propagation for Kafka message headers.
/// Injects <c>traceparent</c> and <c>tracestate</c> headers on the producer side
/// and extracts them on the consumer side to enable distributed tracing across services.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/trace-context/">W3C Trace Context specification</see>.
/// </remarks>
internal static class TraceContextPropagator
{
    private const string TraceParentHeader = "traceparent";
    private const string TraceStateHeader = "tracestate";

    /// <summary>
    /// Injects the current trace context into the supplied headers dictionary.
    /// Adds <c>traceparent</c> and optionally <c>tracestate</c> headers following the W3C Trace Context format.
    /// </summary>
    /// <param name="headers">The existing message headers. If <see langword="null"/>, an empty dictionary is used.</param>
    /// <param name="activity">
    /// The <see cref="Activity"/> whose context to inject.
    /// When <see langword="null"/>, the headers are returned unchanged.
    /// </param>
    /// <returns>A new <see cref="ImmutableDictionary{TKey, TValue}"/> with trace context headers added.</returns>
    internal static ImmutableDictionary<string, string> Inject(ImmutableDictionary<string, string>? headers, Activity? activity)
    {
        headers ??= ImmutableDictionary<string, string>.Empty;

        if (activity is null)
        {
            return headers;
        }

        string flags = activity.Recorded ? "01" : "00";
        string traceparent = $"00-{activity.TraceId}-{activity.SpanId}-{flags}";

        headers = headers.SetItem(TraceParentHeader, traceparent);

        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            headers = headers.SetItem(TraceStateHeader, activity.TraceStateString);
        }

        return headers;
    }

    /// <summary>
    /// Extracts a W3C trace context from the supplied headers dictionary.
    /// Parses the <c>traceparent</c> and optionally <c>tracestate</c> headers.
    /// </summary>
    /// <param name="headers">The message headers to extract trace context from.</param>
    /// <returns>
    /// An <see cref="ActivityContext"/> parsed from the headers, or <see langword="null"/>
    /// if the headers are missing or contain an invalid <c>traceparent</c> value.
    /// </returns>
    internal static ActivityContext? Extract(IReadOnlyDictionary<string, string>? headers)
    {
        if (headers is null || !headers.TryGetValue(TraceParentHeader, out var traceparent))
        {
            return null;
        }

        headers.TryGetValue(TraceStateHeader, out var tracestate);

        if (ActivityContext.TryParse(traceparent, tracestate, out var context))
        {
            return context;
        }

        return null;
    }
}
