// Example usage of the consolidated ForgetMiddleware

// Before consolidation - CONFLICTS!
// modelBuilder.Topic<OrderCreated>(topic =>
// {
//     topic.HasProducer(producer =>
//     {
//         producer.HasAwaitForget(await => await.WithTimeout(TimeSpan.FromSeconds(5)));
//         producer.HasFireForget(); // CONFLICT! Both can be enabled
//     });
// });

// After consolidation - NO CONFLICTS!
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasProducer(producer =>
    {
        // New unified API - choose strategy explicitly
        producer.HasForget(forget =>
        {
            // Option 1: Explicit strategy methods
            forget.UseAwaitForget().WithTimeout(TimeSpan.FromSeconds(5));
            
            // Option 2: Convenience methods
            // forget.WithAwaitForget(TimeSpan.FromSeconds(5));
            
            // Option 3: Fire-forget
            // forget.UseFireForget();
            // or
            // forget.WithFireForget();
        });
        
        // Backward compatibility (with obsolete warnings)
        // producer.HasAwaitForget(await => await.WithTimeout(TimeSpan.FromSeconds(5)));
        // producer.HasFireForget();
    });
});

// Consumer example
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.HasConsumer(consumer =>
    {
        consumer.HasForget(forget =>
        {
            forget.UseFireForget(); // Fire-and-forget for consumers
        });
    });
});
