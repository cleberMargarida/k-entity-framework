using Microsoft.EntityFrameworkCore.Metadata;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring worker-global outbox settings.
/// These settings apply to the single outbox background worker and are not per-message-type.
/// Use <see cref="ModelBuilderExtensions.HasOutboxWorker"/> to access this builder.
/// </summary>
public class OutboxGlobalBuilder(IMutableModel model)
{
    /// <summary>
    /// Sets the polling interval for the outbox background worker.
    /// </summary>
    /// <param name="interval">The polling interval. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
    /// <remarks>
    /// Default is <c>00:00:01</c> (1 second).
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxGlobalBuilder WithPollingInterval(TimeSpan interval)
    {
        model.SetOutboxPollingInterval(interval);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of messages to process in a single poll cycle.
    /// </summary>
    /// <param name="max">The maximum number of messages per poll. Must be greater than zero.</param>
    /// <remarks>
    /// Default is <c>100</c>.
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxGlobalBuilder WithMaxMessagesPerPoll(int max)
    {
        model.SetOutboxMaxMessagesPerPoll(max);
        return this;
    }

    /// <summary>
    /// Configures single-node coordination (default). No cluster awareness —
    /// one worker processes all outbox rows.
    /// </summary>
    /// <remarks>
    /// This is the default strategy. Only needs to be called explicitly to override
    /// a previous <see cref="UseExclusiveNode"/> call.
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxGlobalBuilder UseSingleNode()
    {
        model.SetOutboxCoordinationStrategy(OutboxCoordinationStrategy.SingleNode);
        return this;
    }

    /// <summary>
    /// Configures exclusive-node coordination for clustered deployments.
    /// Only one node processes outbox rows at a time.
    /// </summary>
    /// <remarks>
    /// Currently behaves identically to <see cref="UseSingleNode"/> (placeholder for future implementation).
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    public OutboxGlobalBuilder UseExclusiveNode()
    {
        model.SetOutboxCoordinationStrategy(OutboxCoordinationStrategy.ExclusiveNode);
        return this;
    }
}
