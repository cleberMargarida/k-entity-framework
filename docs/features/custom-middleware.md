# Custom Middleware

The K-Entity-Framework pipeline supports user-defined middleware that participates in message processing alongside the built-in middleware. Custom middleware lets you add cross-cutting concerns such as logging, auditing, header enrichment, or metrics collection without modifying the core pipeline.

## Implementing `IMiddleware<T>`

Create a class that implements `IMiddleware<T>` and set `IsEnabled` to `true`:

```csharp
using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Interfaces;

public class AuditLogMiddleware<T> : IMiddleware<T> where T : class
{
    private readonly ILogger<AuditLogMiddleware<T>> logger;

    public AuditLogMiddleware(ILogger<AuditLogMiddleware<T>> logger)
    {
        this.logger = logger;
    }

    public bool IsEnabled => true;

    public ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Processing message of type {MessageType} with key {Key}",
            typeof(T).Name,
            envelope.Key);

        return ValueTask.FromResult<T?>(envelope.Message);
    }
}
```

> **Important:** `IsEnabled` defaults to `false` on the interface. You **must** return `true` for your middleware to be included in the pipeline. Middleware with `IsEnabled = false` is silently skipped.

## Registration

Register custom middleware on a per-topic basis using the fluent builder API inside `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Topic<OrderEvent>(topic => topic
        .HasProducer(producer => producer
            .HasKey(e => e.OrderId)
            .HasMiddleware<AuditLogMiddleware<OrderEvent>>()
        )
    );
}
```

For consumers:

```csharp
modelBuilder.Topic<OrderEvent>(topic => topic
    .HasConsumer(consumer => consumer
        .HasMiddleware<AuditLogMiddleware<OrderEvent>>()
    )
);
```

## Pipeline Insertion Points

Custom middleware is inserted at a well-defined extension point in each pipeline:

### Producer Pipeline

```
Serializer → [User MW 1] → [User MW 2] → TracePropagation → Forget → Outbox → Producer
```

User middleware runs **after serialization**, so the `Envelope<T>` has its `Payload`, `Key`, and `Headers` populated.

### Consumer Pipeline

```
Subscriber → Consumer → TraceExtraction → Deserializer → [User MW 1] → [User MW 2] → HeaderFilter → Inbox
```

User middleware runs **after deserialization**, so the `Envelope<T>` has the deserialized `Message` available.

## Ordering Guarantees

Multiple middleware registered via successive `HasMiddleware<T>()` calls are chained in **FIFO (first-in, first-out)** order — the first registered middleware runs first:

```csharp
producer
    .HasMiddleware<LoggingMiddleware<OrderEvent>>()   // runs first
    .HasMiddleware<MetricsMiddleware<OrderEvent>>()   // runs second
    .HasMiddleware<TenantMiddleware<OrderEvent>>();    // runs third
```

## Dependency Injection

Custom middleware instances are created using `ActivatorUtilities.CreateInstance`, which means:

- **Constructor injection** is fully supported. Any dependency registered in the DI container can be injected into your middleware constructor.
- **Scoped lifetime** — each DI scope (typically one per message batch / DbContext scope) gets its own middleware instance. Middleware is not shared across scopes.
- **Thread safety** — because each scope gets its own instance, you do not need to worry about concurrent access within a single middleware instance.

```csharp
public class TenantContextMiddleware<T> : IMiddleware<T> where T : class
{
    private readonly ITenantService tenantService;

    public TenantContextMiddleware(ITenantService tenantService)
    {
        this.tenantService = tenantService;
    }

    public bool IsEnabled => true;

    public ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        tenantService.SetCurrentTenant(envelope.Headers["tenant-id"]);
        return ValueTask.FromResult<T?>(envelope.Message);
    }
}
```

## Constraints

- `Envelope<T>` is a **ref struct**. Your `InvokeAsync` method cannot use `async`/`await` on .NET 8 or .NET 9. On .NET 10, `scoped` ref struct parameters in async methods may be supported.
- Custom middleware performs **pre-processing** — it runs before the rest of the pipeline continues. Modifications to the `Message` object (a reference type) are visible to subsequent middleware.
- Custom middleware cannot replace or wrap built-in middleware (serializer, outbox, etc.).
- The `IMiddleware<T>.Next` property is `internal` and managed by the framework. Do not attempt to set or read it.
