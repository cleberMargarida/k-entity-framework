# Forget Middleware (Fire-and-Forget)

K-Entity-Framework includes a **Forget middleware** for producers that enables fire-and-forget message publishing semantics. Instead of waiting for broker acknowledgement or persisting messages through the Outbox pattern, the Forget middleware allows you to trade delivery guarantees for lower latency and reduced overhead.

## When to Use

The Forget middleware is ideal for **non-critical messages** where losing an occasional message is acceptable:

- **Metrics and telemetry** — periodic counters, gauges, or health pings
- **Activity logs** — user click streams, page views, or analytics events
- **Cache invalidation hints** — best-effort notifications that a cache entry changed
- **Heartbeats and keep-alive signals** — periodic liveness probes between services

> **Important**: Forget middleware provides **at-most-once delivery**. Messages may be lost if the producer crashes or the broker is temporarily unreachable. Do not use it for business-critical events that require guaranteed delivery — use the [Outbox middleware](../guides/outbox-pattern.md) instead.

## Strategies

The Forget middleware supports two strategies:

| Strategy | Behaviour | Use Case |
|---|---|---|
| **AwaitForget** (default) | Waits for the produce call up to a configurable timeout, then continues regardless of the outcome. Exceptions are silently swallowed. | Low-priority messages where you still want *best-effort* delivery within a time budget. |
| **FireForget** | Starts the produce call asynchronously and returns immediately without waiting. | Maximum throughput scenarios where latency matters more than delivery. |

## Configuration

### Default (AwaitForget with 30 s timeout)

```csharp
modelBuilder.Topic<TelemetryEvent>(topic =>
{
    topic.HasName("telemetry-events");
    topic.HasProducer(producer =>
    {
        producer.HasKey(msg => msg.Id);
        producer.HasForget(); // AwaitForget, 30 s timeout
    });
});
```

### FireForget

```csharp
modelBuilder.Topic<MetricsEvent>(topic =>
{
    topic.HasName("metrics-events");
    topic.HasProducer(producer =>
    {
        producer.HasKey(msg => msg.Id);
        producer.HasForget(forget => forget.UseFireForget());
    });
});
```

### AwaitForget with custom timeout

```csharp
modelBuilder.Topic<AnalyticsEvent>(topic =>
{
    topic.HasName("analytics-events");
    topic.HasProducer(producer =>
    {
        producer.HasKey(msg => msg.Id);
        producer.HasForget(forget => forget.UseAwaitForget(TimeSpan.FromSeconds(5)));
    });
});
```

## Pipeline Position

The Forget middleware sits between the Outbox middleware and the Producer middleware in the chain-of-responsibility pipeline. When enabled, it wraps the downstream call with the chosen strategy:

```
Produce(message)
  │
  ▼
┌──────────────────────┐
│ Serializer           │  Serialize the message
├──────────────────────┤
│ Trace Propagation    │  Inject distributed tracing headers
├──────────────────────┤
│ Outbox (if enabled)  │  Persist to outbox table (guaranteed delivery)
├──────────────────────┤
│ Forget (if enabled)  │  ◄── Fire-and-forget wrapper
├──────────────────────┤
│ Producer             │  Publish to Kafka broker
└──────────────────────┘
```

> **Note**: Outbox and Forget are **mutually exclusive** in practice. If both are enabled, the Outbox middleware will intercept the message first and persist it to the database, making the Forget middleware a no-op for the initial call. Configure one or the other depending on your delivery requirements.

## Trade-offs

| Aspect | Outbox | Forget |
|---|---|---|
| **Delivery guarantee** | At-least-once | At-most-once |
| **Latency** | Higher (database write + background poll) | Lower (no persistence) |
| **Throughput** | Bounded by DB write speed | Bounded only by producer throughput |
| **Database dependency** | Required | Not required |
| **Message ordering** | Preserved (sequence numbers) | Best-effort |

## See Also

- [Outbox Pattern](../guides/outbox-pattern.md) — guaranteed delivery with the transactional outbox
- [API Reference: ProducerForgetBuilder](../api/K.EntityFrameworkCore.Extensions.MiddlewareBuilders.ProducerForgetBuilder-1.yml)
