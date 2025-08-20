using Confluent.Kafka;
using K.EntityFrameworkCore.Extensions;

namespace K.EntityFrameworkCore.Middlewares;

internal class ConsumerMiddlewareSettings<T>(ClientSettings<T> clientSettings) : MiddlewareSettings<T>
    where T : class
{
    private readonly ConsumerConfig consumerConfig = new(clientSettings.ClientConfig);

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
public class ConsumerRetryMiddlewareSettings<T> : RetryMiddlewareSettings<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerCircuitBreakerMiddlewareSettings<T> : CircuitBreakerMiddlewareSettings<T>
    where T : class
{
}

/// <summary>
/// Consumer-specific configuration options for the BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerBatchMiddlewareSettings<T> : BatchMiddlewareSettings<T>
    where T : class
{
}
