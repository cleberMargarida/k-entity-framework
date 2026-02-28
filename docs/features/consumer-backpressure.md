# Consumer Backpressure: Pause & Resume

K-Entity-Framework includes built-in consumer backpressure management using **watermark-based pause and resume**. When in-memory message channels fill up beyond a configurable threshold, the Kafka consumer is automatically paused to stop fetching new messages. Once channels drain sufficiently, the consumer resumes. This prevents out-of-memory conditions and provides smooth flow control without message loss.

## How It Works

Each message type in K-Entity-Framework has its own bounded in-memory channel that buffers messages between the Kafka consumer poll loop and the processing pipeline. The backpressure mechanism monitors the fill level of these channels using two thresholds:

- **High Water Mark (HWM)**: When the channel fill level reaches this ratio of capacity, the Kafka consumer is **paused**. Pausing stops new message fetches from the broker while keeping the consumer group heartbeat alive.
- **Low Water Mark (LWM)**: When the channel fill level drops to this ratio of capacity, the Kafka consumer is **resumed** and message fetching continues.

```text
Channel Fill Level
┌─────────────────────────────────┐
│                                 │ ← Capacity (MaxBufferedMessages)
│  ───── High Water Mark (80%) ── │ ← Consumer pauses here
│                                 │
│                                 │
│  ───── Low Water Mark (50%) ─── │ ← Consumer resumes here
│                                 │
│                                 │
└─────────────────────────────────┘
```

## Configuration

### Global Defaults

Configure default watermark ratios for all consumers:

```csharp
builder.Services.AddDbContext<MyDbContext>(options => options
    .UseSqlServer("...")
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        
        // Global watermark defaults
        client.Consumer.HighWaterMarkRatio = 0.80; // Pause at 80% full (default)
        client.Consumer.LowWaterMarkRatio = 0.50;  // Resume at 50% full (default)
        client.Consumer.MaxBufferedMessages = 10_000;
    }));
```

### Per-Type Overrides

Override watermark settings for specific message types:

```csharp
modelBuilder.Topic<HighVolumeEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasMaxBufferedMessages(5000);
        consumer.WithHighWaterMark(0.90);  // Pause later — more buffer tolerance
        consumer.WithLowWaterMark(0.30);   // Resume earlier — drain more before resuming
    });
});

modelBuilder.Topic<CriticalPayment>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasMaxBufferedMessages(100);
        consumer.WithHighWaterMark(0.70);  // Pause sooner — conservative
        consumer.WithLowWaterMark(0.50);   // Resume sooner — keep messages flowing
    });
});
```

### Validation Rules

| Parameter | Allowed Range | Default |
|-----------|--------------|---------|
| `HighWaterMarkRatio` | `(0.0, 1.0]` | `0.80` |
| `LowWaterMarkRatio` | `[0.0, 1.0)` | `0.50` |
| `MaxBufferedMessages` | `> 0` | `10,000` |

- High water mark must be greater than low water mark.
- Setting high water mark to `1.0` means the consumer only pauses when the channel is completely full.
- Setting low water mark to `0.0` means the consumer only resumes when the channel is completely empty.

## Shared Consumer Handling

By default, K-Entity-Framework uses a **single shared consumer** for all message types. When multiple message types flow through the same consumer, the pause/resume logic tracks each channel independently:

- **Pause**: If _any_ channel for a shared consumer reaches its high water mark, the consumer is paused.
- **Resume**: The consumer is only resumed when _all_ paused channels have drained below their respective low water marks.

This ensures that a slow-processing message type doesn't cause unbounded buffering while other types continue to fill up.

### Exclusive Connections

When using `HasExclusiveConnection()`, each message type gets its own dedicated consumer. In this case, pause/resume decisions are independent — each consumer is paused and resumed based solely on its own channel's fill level.

```csharp
modelBuilder.Topic<SlowMessage>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasExclusiveConnection(); // Dedicated consumer
        consumer.WithHighWaterMark(0.70);
        consumer.WithLowWaterMark(0.40);
    });
});
```

## Heartbeat During Pause

When a consumer is paused, the poll loop continues to call `Consume` with a short timeout (100ms). This ensures:

1. **Group heartbeats** are sent, preventing the consumer from being kicked out of the consumer group.
2. **Rebalance events** are still processed.
3. **No tight CPU spin** — a `Task.Delay(100ms)` is applied between heartbeat polls.

## Interaction with Backpressure Modes

The watermark-based pause/resume works alongside the existing `ConsumerBackpressureMode` setting:

| Mode | Behavior |
|------|----------|
| `ApplyBackpressure` | Channel write blocks when full. Pause/resume prevents blocking by proactively pausing the consumer before the channel is completely full. |
| `DropOldestMessage` | Channel drops oldest messages when full. Pause/resume reduces drops by pausing the consumer before the channel fills. |
| `DropNewestMessage` | Channel drops newest messages when full. Pause/resume reduces drops by pausing the consumer before the channel fills. |

## Channel Properties

The following properties are available on the internal `Channel` class for diagnostics and monitoring:

| Property | Type | Description |
|----------|------|-------------|
| `Count` | `int` | Current number of buffered messages |
| `Capacity` | `int` | Maximum channel capacity |
| `HighWaterMark` | `int` | Computed threshold count for pausing |
| `LowWaterMark` | `int` | Computed threshold count for resuming |
| `ShouldPause` | `bool` | `true` when `Count >= HighWaterMark` |
| `ShouldResume` | `bool` | `true` when `Count <= LowWaterMark` |
