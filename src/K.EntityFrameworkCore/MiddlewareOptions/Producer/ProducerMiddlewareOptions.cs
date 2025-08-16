using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.MiddlewareOptions.Producer;

/// <summary>
/// Producer-specific configuration options for the RetryMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerRetryMiddlewareOptions<T> : RetryMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the CircuitBreakerMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerCircuitBreakerMiddlewareOptions<T> : CircuitBreakerMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the ThrottleMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerThrottleMiddlewareOptions<T> : ThrottleMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the BatchMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerBatchMiddlewareOptions<T> : BatchMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the AwaitForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerAwaitForgetMiddlewareOptions<T> : AwaitForgetMiddlewareOptions<T>
    where T : class
{
}

/// <summary>
/// Producer-specific configuration options for the FireForgetMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerFireForgetMiddlewareOptions<T> : FireForgetMiddlewareOptions<T>
    where T : class
{
}
