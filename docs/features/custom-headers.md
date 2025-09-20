# Custom Headers

K-Entity-Framework supports extracting custom headers from your messages for routing, filtering, and metadata purposes.

## Configuration

Configure custom headers in your [`OnModelCreating`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext.onmodelcreating?view=efcore-8.0) method:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Topic<OrderCreated>(topic =>
    {
        topic.HasProducer(producer =>
        {
            producer.HasKey(o => o.CustomerId);
            
            // Configure custom headers
            producer.HasHeader("region", o => o.Customer.Region);
            producer.HasHeader("priority", o => o.Priority);
            producer.HasHeader("amount", o => o.Amount);
        });
    });
}
```

## Complex Logic

For complex header logic, use computed properties in your message classes:

```csharp
public class OrderCreated
{
    public decimal Amount { get; set; }
    public Customer Customer { get; set; }
    
    // Computed properties for headers
    public bool IsHighValue => Amount > 1000;
    public string CustomerTier => 
        Customer.TotalOrders > 100 ? "premium" : 
        Customer.TotalOrders > 50 ? "standard" : "basic";
}

// Use in header configuration
producer.HasHeader("is-high-value", o => o.IsHighValue);
producer.HasHeader("customer-tier", o => o.CustomerTier);
```
> [!IMPORTANT]
> Use computed properties for complex header logic, but keep them cheap to compute — header expressions are evaluated during production and can affect producer performance if they run expensive logic.

## Built-in Headers

K-Entity-Framework automatically adds:
- `$type` - Compile-time type of the message
- `$runtimeType` - Runtime type (if different from compile-time)

## Examples

### E-commerce
```csharp
public class OrderCreated
{
    public string Priority => TotalAmount > 1000 ? "high" : "normal";
}

producer.HasHeader("region", o => o.Customer.Region);
producer.HasHeader("priority", o => o.Priority);
```

### IoT Sensors
```csharp
public class SensorReading
{
    public string AlertLevel => 
        Value > CriticalThreshold ? "critical" :
        Value > WarningThreshold ? "warning" : "normal";
}

producer.HasHeader("device-type", s => s.DeviceType);
producer.HasHeader("alert-level", s => s.AlertLevel);
```

Headers are compiled once and cached for performance. Use computed properties for complex logic since expression trees have limitations with ternary operators in header expressions.

## Consumer Header Filtering

You can configure consumers to filter messages based on header values using exact string matching. The header filter middleware runs after deserialization but before inbox processing, allowing you to selectively process messages based on their header content.

```csharp
// Multi-tenant application - filter by tenant
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        // Only process orders for tenant "ACME"
        consumer.HasHeaderFilter("tenant", "ACME");
        
        // Multiple filters can be applied
        consumer.HasHeaderFilter("region", "US-East");
    });
});

// Event filtering by priority
modelBuilder.Topic<NotificationEvent>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        // Only process high-priority notifications
        consumer.HasHeaderFilter("priority", "high");
    });
});
```

#### Multi-Tenant Example

Here's a comprehensive example of using header filtering for multi-tenant message processing:

```csharp
public class TenantOrderCreated
{
    public int OrderId { get; set; }
    public string TenantId { get; set; }
    public string Region { get; set; }
    public decimal Amount { get; set; }
}

// Producer configuration - add tenant info to headers
modelBuilder.Topic<TenantOrderCreated>(topic =>
{
    topic.HasName("tenant-orders");
    topic.HasProducer(producer =>
    {
        producer.HasKey(order => order.OrderId.ToString());
        producer.HasHeader("tenant", order => order.TenantId);
        producer.HasHeader("region", order => order.Region);
    });
    
    topic.HasConsumer(consumer =>
    {
        // Filter to only process orders for specific tenant and region
        consumer.HasHeaderFilter("tenant", "CLIENT-A");
        consumer.HasHeaderFilter("region", "US-West");
        
        // Configure inbox for deduplication
        consumer.HasInbox(inbox =>
        {
            inbox.HasDeduplicateProperties(order => new { order.OrderId, order.TenantId });
        });
    });
});
```

> [!NOTE]
> Header filters use exact string matching (case-insensitive). If any filter doesn't match, the message is discarded and does not proceed to the next middleware or business logic. All configured filters must pass for the message to be processed.
