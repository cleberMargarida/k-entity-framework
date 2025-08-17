// EXAMPLE: Type-based Serialization Strategy Plugins (No Strings, No Reflection)

/*
 * ===================================================================
 * CORE PRINCIPLE: Use Types as Keys, Not Strings
 * ===================================================================
 * 
 * Instead of using magic strings like "NewtonsoftJson", we use the 
 * options type itself as the unique identifier. This provides:
 * 
 * ✅ Compile-time safety
 * ✅ No magic strings
 * ✅ No runtime reflection in hot paths
 * ✅ Better performance
 * ✅ IntelliSense support
 */

/*
 * ===================================================================
 * EXAMPLE 1: Newtonsoft.Json Plugin (External NuGet Package)
 * ===================================================================
 * 
 * File: K.EntityFrameworkCore.Serialization.NewtonsoftJson/NewtonsoftJsonPlugin.cs
 * Package: K.EntityFrameworkCore.Serialization.NewtonsoftJson
 * Dependencies: Newtonsoft.Json, K.EntityFrameworkCore
 */

/*
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace K.EntityFrameworkCore.Serialization.NewtonsoftJson;

// The options type IS the strategy identifier
public class NewtonsoftJsonOptions : JsonSerializerSettings
{
    public NewtonsoftJsonOptions()
    {
        NullValueHandling = NullValueHandling.Ignore;
        Formatting = Formatting.None;
        DateFormatHandling = DateFormatHandling.IsoDateFormat;
    }
}

// Generic serializer implementation
public class NewtonsoftJsonSerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly NewtonsoftJsonOptions _options;

    public NewtonsoftJsonSerializer(NewtonsoftJsonOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public byte[] Serialize(T message)
    {
        var json = JsonConvert.SerializeObject(message, _options);
        return Encoding.UTF8.GetBytes(json);
    }

    public T? Deserialize(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(json, _options);
    }
}

// Plugin registration
public static class NewtonsoftJsonPlugin
{
    public static void Register()
    {
        // Register using the options type as the key
        SerializationExtensions.RegisterStrategy<NewtonsoftJsonSerializer<>, NewtonsoftJsonOptions>(
            () => new NewtonsoftJsonOptions()
        );
    }
}

// Strongly-typed extension method
public static class TopicTypeBuilderExtensions
{
    public static TopicTypeBuilder<T> UseNewtonsoftJson<T>(
        this TopicTypeBuilder<T> builder, 
        Action<NewtonsoftJsonOptions>? configure = null) where T : class
    {
        return builder.UseSerializer<NewtonsoftJsonOptions>(configure);
    }
}
*/

/*
 * ===================================================================
 * EXAMPLE 2: MessagePack Plugin (External NuGet Package)
 * ===================================================================
 */

/*
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using MessagePack;

namespace K.EntityFrameworkCore.Serialization.MessagePack;

// Custom options type that wraps MessagePack options
public class MessagePackOptions
{
    public MessagePackSerializerOptions SerializerOptions { get; set; } = MessagePackSerializerOptions.Standard;
    public bool UseCompression { get; set; } = false;
}

public class MessagePackSerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly MessagePackOptions _options;

    public MessagePackSerializer(MessagePackOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public byte[] Serialize(T message)
    {
        var data = MessagePack.MessagePackSerializer.Serialize(message, _options.SerializerOptions);
        
        if (_options.UseCompression)
        {
            // Apply compression if needed
            return CompressData(data);
        }
        
        return data;
    }

    public T? Deserialize(byte[] data)
    {
        if (_options.UseCompression)
        {
            data = DecompressData(data);
        }
        
        return MessagePack.MessagePackSerializer.Deserialize<T>(data, _options.SerializerOptions);
    }
    
    private byte[] CompressData(byte[] data) => data; // Implement compression
    private byte[] DecompressData(byte[] data) => data; // Implement decompression
}

public static class MessagePackPlugin
{
    public static void Register()
    {
        SerializationExtensions.RegisterStrategy<MessagePackSerializer<>, MessagePackOptions>(
            () => new MessagePackOptions()
        );
    }
}

public static class TopicTypeBuilderExtensions
{
    public static TopicTypeBuilder<T> UseMessagePack<T>(
        this TopicTypeBuilder<T> builder, 
        Action<MessagePackOptions>? configure = null) where T : class
    {
        return builder.UseSerializer<MessagePackOptions>(configure);
    }
}
*/

/*
 * ===================================================================
 * EXAMPLE 3: Custom Binary Serializer (In-App Implementation)
 * ===================================================================
 */

/*
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;

namespace MyApp.Serialization;

// Simple options type - this IS the strategy key
public class CustomBinaryOptions
{
    public bool UseCompression { get; set; } = false;
    public int CompressionLevel { get; set; } = 6;
    public bool IncludeTypeInfo { get; set; } = true;
}

public class CustomBinarySerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly CustomBinaryOptions _options;

    public CustomBinarySerializer(CustomBinaryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public byte[] Serialize(T message)
    {
        // Custom binary serialization logic
        using var stream = new MemoryStream();
        
        if (_options.IncludeTypeInfo)
        {
            // Write type information
        }
        
        // Serialize message
        
        if (_options.UseCompression)
        {
            // Apply compression
        }
        
        return stream.ToArray();
    }

    public T? Deserialize(byte[] data)
    {
        // Custom binary deserialization logic
        using var stream = new MemoryStream(data);
        
        if (_options.UseCompression)
        {
            // Decompress first
        }
        
        if (_options.IncludeTypeInfo)
        {
            // Read type information
        }
        
        // Deserialize message
        return default(T);
    }
}

// Registration in Startup.cs:
public void ConfigureServices(IServiceCollection services)
{
    // Register custom strategy - no strings!
    SerializationExtensions.RegisterStrategy<CustomBinarySerializer<>, CustomBinaryOptions>(
        () => new CustomBinaryOptions { UseCompression = true }
    );
}
*/

/*
 * ===================================================================
 * USAGE PATTERNS - TYPE-BASED API
 * ===================================================================
 */

/*
// 1. Built-in System.Text.Json (no registration needed)
modelBuilder.Topic<Order>(topic =>
{
    topic.UseJsonSerializer(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });
    // Behind the scenes: Uses JsonSerializerOptions as the type key
});

// 2. External strategy using options type as key
modelBuilder.Topic<Order>(topic =>
{
    topic.UseSerializer<NewtonsoftJsonOptions>(options =>
    {
        options.DateFormatString = "yyyy-MM-dd";
        options.NullValueHandling = NullValueHandling.Include;
    });
});

// 3. Convenience extension methods (type-safe)
modelBuilder.Topic<Order>(topic =>
{
    topic.UseNewtonsoftJson(options =>
    {
        options.DateFormatString = "yyyy-MM-dd";
    });
});

// 4. Custom in-app serializer
modelBuilder.Topic<Order>(topic =>
{
    topic.UseSerializer<CustomBinaryOptions>(options =>
    {
        options.UseCompression = true;
        options.CompressionLevel = 9;
    });
});

// 5. Mixed strategies per topic type - all type-safe!
modelBuilder.Topic<FastEvent>(topic => topic.UseSerializer<MessagePackOptions>());      // Performance
modelBuilder.Topic<ApiData>(topic => topic.UseJsonSerializer());                        // Readability  
modelBuilder.Topic<LegacyData>(topic => topic.UseSerializer<NewtonsoftJsonOptions>());  // Compatibility
modelBuilder.Topic<BinaryData>(topic => topic.UseSerializer<CustomBinaryOptions>());   // Custom logic
*/

/*
 * ===================================================================
 * KEY ADVANTAGES OF TYPE-BASED APPROACH
 * ===================================================================
 * 
 * 1. **No Magic Strings**: Types are compile-time checked
 * 2. **No Runtime Reflection**: Serializers created once and cached
 * 3. **IntelliSense Support**: IDE knows available options
 * 4. **Refactoring Safe**: Rename types, get compile errors instead of runtime failures
 * 5. **Performance**: No string lookups or Type.GetType() calls in hot paths
 * 6. **Type Safety**: Can't accidentally use wrong options type
 * 7. **Clean API**: UseSerializer<TOptions>() is clear and concise
 * 
 * BEFORE (String-based):
 * topic.UseSerializer("NewtonsoftJson", options => { ... }); // Magic string!
 * 
 * AFTER (Type-based):
 * topic.UseSerializer<NewtonsoftJsonOptions>(options => { ... }); // Compile-time safe!
 */
