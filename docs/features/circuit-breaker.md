# Consumer Circuit Breaker

K-Entity-Framework includes a **circuit breaker** for consumers that automatically pauses consumption when persistent processing failures are detected. This prevents a failing downstream system from being overwhelmed with retries, giving it time to recover before consumption resumes.

## How It Works

The circuit breaker follows the classic three-state pattern:

```text
                      failures ≥ threshold
  ┌────────┐       ─────────────────────────►     ┌────────┐
  │ Closed │                                      │  Open  │
  │        │       ◄─────────────────────────     │        │
  └────────┘         successes ≥ active            └────────┘
       ▲             threshold (via HalfOpen)          │
       │                                               │
       │              reset interval                   │
       │              elapsed                          ▼
       │            ┌──────────┐                       │
       └────────────│ HalfOpen │◄──────────────────────┘
                    └──────────┘
                         │
                         │ failure
                         ▼
                    ┌────────┐
                    │  Open  │
                    └────────┘
```

### States

| State      | Description |
|------------|-------------|
| **Closed** | Normal operation. All messages are consumed and processed. Failures are tracked in a sliding window. |
| **Open**   | Consumption is paused. The consumer maintains its Kafka group heartbeat but does not fetch new messages. After the reset interval elapses, transitions to HalfOpen. |
| **HalfOpen** | Probing state. The consumer resumes and processes messages to test recovery. On success (meeting the active threshold), transitions back to Closed. On failure, returns to Open. |

### Sliding Window

The circuit breaker uses a fixed-size sliding window to track the most recent outcomes. Only failures within this window count toward the trip threshold — older failures naturally age out as new outcomes are recorded. This prevents a few scattered failures over a long period from tripping the circuit.

## Configuration

Enable the circuit breaker on any consumer using the `HasCircuitBreaker()` method:

### Default Configuration

```csharp
modelBuilder.Topic<OrderEvent>(topic =>
{
    topic.Consumer(consumer =>
    {
        consumer.HasCircuitBreaker();
    });
});
```

Default values:
- **TripThreshold**: 5 — failures in the window to trip the circuit
- **WindowSize**: 10 — size of the sliding window
- **ResetInterval**: 30 seconds — time in Open before probing
- **ActiveThreshold**: 1 — successes in HalfOpen to close the circuit

### Custom Configuration

```csharp
modelBuilder.Topic<OrderEvent>(topic =>
{
    topic.Consumer(consumer =>
    {
        consumer.HasCircuitBreaker(cb =>
        {
            cb.TripAfter(3)               // Trip after 3 failures
              .HasWindowSize(20)          // Track last 20 outcomes
              .HasResetInterval(TimeSpan.FromMinutes(1))  // Wait 1 minute before probing
              .HasActiveThreshold(2);     // Require 2 successes to close
        });
    });
});
```

## Integration with Consumer Poll Loop

The circuit breaker integrates directly into the `ConsumerPollRegistry` poll loop:

1. **On processing success**: `RecordSuccess()` is called, recording a success in the sliding window (Closed state) or counting toward the active threshold (HalfOpen state).

2. **On unexpected processing failure** (non-`ConsumeException`, non-`OperationCanceledException`): `RecordFailure()` is called. If the failure count in the sliding window meets the trip threshold, the circuit opens.

3. **When the circuit is Open**: The Kafka consumer is paused (separate from backpressure pause). Short-timeout `Consume` calls keep the group heartbeat alive. After the reset interval, `AllowRequest()` transitions to HalfOpen and the consumer resumes.

4. **Without circuit breaker**: The original behavior is preserved — unexpected failures stop the consumer immediately.

### Coexistence with Backpressure

The circuit breaker pause/resume is **independent** from the [backpressure-based pause/resume](consumer-backpressure.md). Both mechanisms can be active simultaneously:

- Backpressure pauses when channels are full (high water mark) and resumes when drained (low water mark)
- Circuit breaker pauses when failures exceed the threshold and resumes when the reset interval elapses

The consumer is only fully resumed when **neither** mechanism requires it to be paused.

## Example: Protecting Against Database Outages

```csharp
modelBuilder.Topic<InventoryUpdate>(topic =>
{
    topic.Consumer(consumer =>
    {
        // If the database is down, stop hammering it
        consumer.HasCircuitBreaker(cb =>
        {
            cb.TripAfter(5)
              .HasWindowSize(10)
              .HasResetInterval(TimeSpan.FromSeconds(30))
              .HasActiveThreshold(1);
        });
    });
});
```

When the database goes down:
1. Processing failures accumulate in the sliding window
2. After 5 failures in the last 10 attempts, the circuit opens
3. The consumer pauses for 30 seconds
4. After 30 seconds, the circuit enters HalfOpen and the consumer resumes
5. If the next message processes successfully, the circuit closes and normal operation continues
6. If it fails again, the circuit reopens for another 30 seconds
