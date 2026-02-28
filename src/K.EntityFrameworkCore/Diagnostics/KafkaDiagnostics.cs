using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace K.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Provides centralized OpenTelemetry instrumentation for K.EntityFrameworkCore.
/// Exposes an <see cref="ActivitySource"/> for distributed tracing and a <see cref="Meter"/>
/// with pre-defined counters and histograms for observability.
/// </summary>
internal static class KafkaDiagnostics
{
    /// <summary>
    /// The instrumentation name used for both the <see cref="ActivitySource"/> and <see cref="Meter"/>.
    /// </summary>
    internal const string Name = "K.EntityFrameworkCore";

    private static readonly string Version =
        typeof(KafkaDiagnostics).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    /// <summary>
    /// The <see cref="ActivitySource"/> used to create tracing activities for produce, consume, and outbox operations.
    /// </summary>
    internal static readonly ActivitySource Source = new(Name, Version);

    /// <summary>
    /// The <see cref="Meter"/> used to record metrics for messaging operations.
    /// </summary>
    internal static readonly Meter Meter = new(Name, Version);

    /// <summary>
    /// Counts the total number of messages produced to Kafka.
    /// </summary>
    internal static readonly Counter<long> MessagesProduced =
        Meter.CreateCounter<long>("k_ef.messages.produced", description: "Total messages produced to Kafka");

    /// <summary>
    /// Counts the total number of messages consumed from Kafka.
    /// </summary>
    internal static readonly Counter<long> MessagesConsumed =
        Meter.CreateCounter<long>("k_ef.messages.consumed", description: "Total messages consumed from Kafka");

    /// <summary>
    /// Counts the total number of duplicate messages filtered by the inbox deduplication mechanism.
    /// </summary>
    internal static readonly Counter<long> InboxDuplicatesFiltered =
        Meter.CreateCounter<long>("k_ef.inbox.duplicates_filtered", description: "Total duplicate messages filtered by inbox");

    /// <summary>
    /// Records the duration in milliseconds of outbox message publish operations.
    /// </summary>
    internal static readonly Histogram<double> OutboxPublishDuration =
        Meter.CreateHistogram<double>("k_ef.outbox.publish_duration", unit: "ms", description: "Duration of outbox publish operations");

    /// <summary>
    /// Registers an observable gauge that reports the number of pending outbox messages awaiting publishing.
    /// </summary>
    /// <param name="observeValue">A callback that returns the current count of pending outbox messages.</param>
    internal static void RegisterOutboxPendingGauge(Func<int> observeValue)
    {
        Meter.CreateObservableGauge(
            "k_ef.outbox.pending",
            () => observeValue(),
            description: "Number of pending outbox messages");
    }
}
