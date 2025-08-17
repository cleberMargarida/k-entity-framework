// EXAMPLE: How to create external serialization strategy plugins

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

public class NewtonsoftJsonSerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftJsonSerializer(JsonSerializerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public byte[] Serialize(T message)
    {
        var json = JsonConvert.SerializeObject(message, _settings);
        return Encoding.UTF8.GetBytes(json);
    }

    public T? Deserialize(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(json, _settings);
    }
}

public static class NewtonsoftJsonPlugin
{
    public static void Register()
    {
        SerializationExtensions.RegisterStrategy<NewtonsoftJsonSerializer<>, JsonSerializerSettings>(
            "NewtonsoftJson",
            () => new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            },
            "Newtonsoft.Json serialization strategy"
        );
    }
}

// Extension method for convenience
public static class TopicTypeBuilderExtensions
{
    public static TopicTypeBuilder<T> UseNewtonsoftJson<T>(
        this TopicTypeBuilder<T> builder, 
        Action<JsonSerializerSettings>? configure = null) where T : class
    {
        return builder.UseSerializer("NewtonsoftJson", options =>
        {
            if (options is JsonSerializerSettings settings && configure != null)
            {
                configure(settings);
            }
        });
    }
}

// Usage in Startup.cs:
// NewtonsoftJsonPlugin.Register();
*/

/*
 * ===================================================================
 * EXAMPLE 2: MessagePack Plugin (External NuGet Package)
 * ===================================================================
 * 
 * File: K.EntityFrameworkCore.Serialization.MessagePack/MessagePackPlugin.cs
 * Package: K.EntityFrameworkCore.Serialization.MessagePack
 * Dependencies: MessagePack, K.EntityFrameworkCore
 */

/*
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using MessagePack;

namespace K.EntityFrameworkCore.Serialization.MessagePack;

public class MessagePackSerializer<T> : IMessageSerializer<T> where T : class
{
    private readonly MessagePackSerializerOptions _options;

    public MessagePackSerializer(MessagePackSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public byte[] Serialize(T message)
    {
        return MessagePack.MessagePackSerializer.Serialize(message, _options);
    }

    public T? Deserialize(byte[] data)
    {
        return MessagePack.MessagePackSerializer.Deserialize<T>(data, _options);
    }
}

public static class MessagePackPlugin
{
    public static void Register()
    {
        SerializationExtensions.RegisterStrategy<MessagePackSerializer<>, MessagePackSerializerOptions>(
            "MessagePack",
            () => MessagePackSerializerOptions.Standard,
            "MessagePack binary serialization strategy"
        );
    }
}

// Extension method for convenience
public static class TopicTypeBuilderExtensions
{
    public static TopicTypeBuilder<T> UseMessagePack<T>(
        this TopicTypeBuilder<T> builder, 
        Action<MessagePackSerializerOptions>? configure = null) where T : class
    {
        return builder.UseSerializer("MessagePack", options =>
        {
            if (options is MessagePackSerializerOptions packOptions && configure != null)
            {
                configure(packOptions);
            }
        });
    }
}

// Usage in Startup.cs:
// MessagePackPlugin.Register();
*/

/*
 * ===================================================================
 * EXAMPLE 3: Protocol Buffers Plugin (External NuGet Package)
 * ===================================================================
 * 
 * File: K.EntityFrameworkCore.Serialization.Protobuf/ProtobufPlugin.cs
 * Package: K.EntityFrameworkCore.Serialization.Protobuf
 * Dependencies: Google.Protobuf, K.EntityFrameworkCore
 */

/*
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using Google.Protobuf;

namespace K.EntityFrameworkCore.Serialization.Protobuf;

public class ProtobufOptions
{
    public bool IgnoreUnknownFields { get; set; } = true;
    public int RecursionLimit { get; set; } = 100;
}

public class ProtobufSerializer<T> : IMessageSerializer<T> 
    where T : class, IMessage<T>, new()
{
    private readonly ProtobufOptions _options;

    public ProtobufSerializer(ProtobufOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public byte[] Serialize(T message)
    {
        return message.ToByteArray();
    }

    public T? Deserialize(byte[] data)
    {
        var message = new T();
        return message.Descriptor.Parser.ParseFrom(data) as T;
    }
}

public static class ProtobufPlugin
{
    public static void Register()
    {
        SerializationExtensions.RegisterStrategy<ProtobufSerializer<>, ProtobufOptions>(
            "Protobuf",
            () => new ProtobufOptions(),
            "Protocol Buffers binary serialization strategy"
        );
    }
}

// Extension method for convenience
public static class TopicTypeBuilderExtensions
{
    public static TopicTypeBuilder<T> UseProtobuf<T>(
        this TopicTypeBuilder<T> builder, 
        Action<ProtobufOptions>? configure = null) 
        where T : class, IMessage<T>, new()
    {
        return builder.UseSerializer("Protobuf", options =>
        {
            if (options is ProtobufOptions protobufOptions && configure != null)
            {
                configure(protobufOptions);
            }
        });
    }
}

// Usage in Startup.cs:
// ProtobufPlugin.Register();
*/

/*
 * ===================================================================
 * EXAMPLE 4: Custom Binary Serializer (In-App Implementation)
 * ===================================================================
 */

/*
using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using System.Runtime.Serialization.Formatters.Binary;

namespace MyApp.Serialization;

public class CustomBinaryOptions
{
    public bool UseCompression { get; set; } = false;
    public int CompressionLevel { get; set; } = 6;
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
        using var stream = new MemoryStream();
        
        if (_options.UseCompression)
        {
            // Add compression logic here
        }
        
        var formatter = new BinaryFormatter();
        formatter.Serialize(stream, message);
        return stream.ToArray();
    }

    public T? Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        
        if (_options.UseCompression)
        {
            // Add decompression logic here
        }
        
        var formatter = new BinaryFormatter();
        return formatter.Deserialize(stream) as T;
    }
}

// Registration in Startup.cs:
public void ConfigureServices(IServiceCollection services)
{
    // Register custom strategy
    SerializationExtensions.RegisterStrategy<CustomBinarySerializer<>, CustomBinaryOptions>(
        "CustomBinary",
        () => new CustomBinaryOptions { UseCompression = true },
        "Custom binary serialization with optional compression"
    );
}

// Usage:
modelBuilder.Topic<MyMessage>(topic =>
{
    topic.UseSerializer("CustomBinary", options =>
    {
        var binaryOptions = (CustomBinaryOptions)options;
        binaryOptions.UseCompression = true;
        binaryOptions.CompressionLevel = 9;
    });
});
*/

/*
 * ===================================================================
 * USAGE PATTERNS
 * ===================================================================
 */

/*
// 1. Using built-in System.Text.Json (no registration needed)
modelBuilder.Topic<Order>(topic =>
{
    topic.UseJsonSerializer(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });
});

// 2. Using registered external strategy
modelBuilder.Topic<Order>(topic =>
{
    topic.UseSerializer("NewtonsoftJson", options =>
    {
        var settings = (JsonSerializerSettings)options;
        settings.DateFormatString = "yyyy-MM-dd";
    });
});

// 3. Using convenience extension methods (if provided by plugin)
modelBuilder.Topic<Order>(topic =>
{
    topic.UseNewtonsoftJson(settings =>
    {
        settings.DateFormatString = "yyyy-MM-dd";
    });
});

// 4. Mixed strategies per topic type
modelBuilder.Topic<FastEvent>(topic => topic.UseMessagePack());         // Performance
modelBuilder.Topic<ApiData>(topic => topic.UseJsonSerializer());        // Readability  
modelBuilder.Topic<LegacyData>(topic => topic.UseNewtonsoftJson());     // Compatibility
modelBuilder.Topic<BinaryData>(topic => topic.UseSerializer("Custom")); // Custom logic
*/
