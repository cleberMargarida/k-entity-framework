// Deduplication Usage Examples for K.EntityFrameworkCore

using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Examples
{
    // Example message types
    public class OrderPlaced
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PlacedAt { get; set; }
    }

    public class UserRegistered
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }

    public class PaymentProcessed
    {
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    public class MyBrokerContext : DbContext
    {
        public Topic<OrderPlaced> OrderEvents { get; set; }
        public Topic<UserRegistered> UserEvents { get; set; }
        public Topic<PaymentProcessed> PaymentEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Example 1: Deduplicate by single property (Order ID)
            // This will ensure no duplicate orders are processed even if the same message is received multiple times
            modelBuilder.Entity<OrderPlaced>()
                .HasConsumer(consumer => consumer
                    .HasInbox(inbox => inbox
                        .DeduplicateBy(order => order.OrderId)  // Use OrderId for deduplication
                        .UseDeduplicationTimeWindow(TimeSpan.FromHours(24)) // Keep dedup records for 24 hours
                    )
                );

            // Example 2: Deduplicate by composite values (User ID + Email)
            // This prevents duplicate registrations for the same user/email combination
            modelBuilder.Entity<UserRegistered>()
                .HasConsumer(consumer => consumer
                    .HasInbox(inbox => inbox
                        .DeduplicateBy(user => new { user.UserId, user.Email }) // Composite key
                        .UseDeduplicationTimeWindow(TimeSpan.FromDays(7)) // Keep records for 7 days
                    )
                );

            // Example 3: Deduplicate by transaction ID for financial operations
            // Critical for payment processing to prevent double charging
            modelBuilder.Entity<PaymentProcessed>()
                .HasConsumer(consumer => consumer
                    .HasInbox(inbox => inbox
                        .DeduplicateBy(payment => payment.TransactionId) // Unique transaction ID
                        .UseDeduplicationTimeWindow(TimeSpan.FromDays(30)) // Keep records for 30 days for auditing
                    )
                );

            // Example 4: Deduplicate by multiple fields with calculated values
            // This shows more complex deduplication logic
            modelBuilder.Entity<PaymentProcessed>()
                .HasName("payment-events-complex") // Different topic for this example
                .HasConsumer(consumer => consumer
                    .HasInbox(inbox => inbox
                        .DeduplicateBy(payment => new 
                        { 
                            payment.OrderId, 
                            payment.Amount,
                            // You can include calculated fields
                            DateOnly = payment.ProcessedAt.Date 
                        })
                        .UseDeduplicationTimeWindow(TimeSpan.FromHours(2)) // Short window for payment processing
                    )
                );

            base.OnModelCreating(modelBuilder);
        }
    }

    // Usage in application
    public class OrderService
    {
        private readonly MyBrokerContext _context;

        public OrderService(MyBrokerContext context)
        {
            _context = context;
        }

        public async Task ProcessOrdersAsync()
        {
            // Consumer will automatically deduplicate based on OrderId
            // If the same OrderPlaced message is received multiple times,
            // only the first one will be processed
            await foreach (var orderEnvelope in _context.OrderEvents.ConsumeAsync())
            {
                var order = orderEnvelope.Unseal();
                if (order != null)
                {
                    Console.WriteLine($"Processing unique order: {order.OrderId}");
                    // Process the order...
                    
                    // Commit after successful processing
                    await _context.OrderEvents.CommitAsync();
                }
            }
        }
    }
}

/*
How it works internally:

1. Expression Tree Compilation:
   - DeduplicateBy(order => order.OrderId) is compiled into a fast Func<T, object>
   - Similar to Producer Key implementation, uses ParameterReplacer for expression tree manipulation
   - Compiled once and reused for performance

2. xxHash64 Algorithm:
   - Extracted values are serialized to JSON
   - JSON bytes are hashed using xxHash64 for speed
   - 64-bit hash is converted to GUID for database storage

3. Database Storage:
   - Hash is stored as GUID in InboxMessage table
   - Duplicate detection is a simple primary key lookup
   - Old records are cleaned up based on DeduplicationTimeWindow

4. Performance Benefits:
   - xxHash64 is extremely fast (multiple GB/s)
   - Expression compilation happens once at startup
   - Database lookup is O(1) using primary key
   - No need to store actual message content for deduplication

5. Flexibility:
   - Single property: x => x.OrderId
   - Multiple properties: x => new { x.UserId, x.Email }
   - Calculated values: x => new { x.OrderId, DateOnly = x.Date.Date }
   - Complex expressions: x => x.Items.Sum(i => i.Price)
*/
