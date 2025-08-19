using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.MiddlewareOptions.Consumer;

internal class ConsumerMiddlewareOptions<T>(ClientOptions<T> clientOptions) : MiddlewareOptions<T>
    where T : class
{
    private readonly ConsumerConfig consumerConfig = new(clientOptions.ClientConfig);

    public string GroupId 
    { 
        get => consumerConfig.GroupId ??= AppDomain.CurrentDomain.FriendlyName; 
        set => consumerConfig.GroupId = value; 
    }

    public IEnumerable<KeyValuePair<string, string>> ConsumerConfig => consumerConfig;
}

/// <summary>
/// Consumer-specific configuration options for the RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerRetryMiddlewareOptions<T> : RetryMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerCircuitBreakerMiddlewareOptions<T> : CircuitBreakerMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerBatchMiddlewareOptions<T> : BatchMiddlewareOptions<T>
    where T : class
{
}
