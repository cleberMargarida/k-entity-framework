// USAGE EXAMPLES FOR ENHANCED SERIALIZATION MIDDLEWARE

/*
 * Example 1: Using System.Text.Json (Default)
 */
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseJsonSerializer(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.WriteIndented = true;
    });
    
    topic.HasProducer(producer => { /* producer config */ });
    topic.HasConsumer(consumer => { /* consumer config */ });
});

/*
 * Example 2: Using Newtonsoft.Json
 * Note: Requires Newtonsoft.Json NuGet package to be installed
 */
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseNewtonsoftJson(settings =>
    {
        // Cast to JsonSerializerSettings when Newtonsoft.Json is available
        var jsonSettings = (JsonSerializerSettings)settings;
        jsonSettings.NullValueHandling = NullValueHandling.Include;
        jsonSettings.Formatting = Formatting.Indented;
        jsonSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
    });
    
    topic.HasProducer(producer => { /* producer config */ });
    topic.HasConsumer(consumer => { /* consumer config */ });
});

/*
 * Example 3: Using MessagePack
 * Note: Requires MessagePack NuGet package to be installed
 */
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseMessagePack(options =>
    {
        // Cast to MessagePackSerializerOptions when MessagePack is available
        var packOptions = (MessagePackSerializerOptions)options;
        // Configure MessagePack-specific options here
    });
    
    topic.HasProducer(producer => { /* producer config */ });
    topic.HasConsumer(consumer => { /* consumer config */ });
});

/*
 * Example 4: Multiple Topics with Different Serialization
 */
// Orders use JSON
modelBuilder.Topic<OrderCreated>(topic =>
{
    topic.UseJsonSerializer(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
});

// Events use MessagePack for performance
modelBuilder.Topic<HighVolumeEvent>(topic =>
{
    topic.UseMessagePack();
});

// Legacy messages use Newtonsoft.Json for compatibility
modelBuilder.Topic<LegacyMessage>(topic =>
{
    topic.UseNewtonsoftJson();
});
