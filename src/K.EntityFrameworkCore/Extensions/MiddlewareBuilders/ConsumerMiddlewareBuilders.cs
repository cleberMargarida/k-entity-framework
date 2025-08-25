using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Inbox;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring InboxMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class InboxBuilder<T>(InboxMiddlewareSettings<T> settings) where T : class
{
    public void DeduplicateBy(Func<T, object> value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Sets the timeout for duplicate message detection.
    /// </summary>
    /// <param name="timeout">The duplicate detection timeout.</param>
    /// <returns>The builder instance.</returns>
    public InboxBuilder<T> UseDeduplicationTimeWindow(TimeSpan timeout)
    {
        settings.DeduplicationTimeWindow = timeout;
        return this;
    }
}

/// <summary>
/// Fluent builder for configuring consumer ForgetMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsumerForgetBuilder<T>(ConsumerForgetMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the forget strategy to AwaitForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> UseAwaitForget()
    {
        settings.Strategy = ForgetStrategy.AwaitForget;
        return this;
    }

    /// <summary>
    /// Sets the forget strategy to FireForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> UseFireForget()
    {
        settings.Strategy = ForgetStrategy.FireForget;
        return this;
    }

    /// <summary>
    /// Sets the timeout duration for awaiting message processing.
    /// Only applies when using AwaitForget strategy.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> WithTimeout(TimeSpan timeout)
    {
        settings.Timeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures the middleware for AwaitForget strategy with optional timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration for awaiting processing.</param>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> WithAwaitForget(TimeSpan? timeout = null)
    {
        settings.Strategy = ForgetStrategy.AwaitForget;
        if (timeout.HasValue)
        {
            settings.Timeout = timeout.Value;
        }
        return this;
    }

    /// <summary>
    /// Configures the middleware for FireForget strategy.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ConsumerForgetBuilder<T> WithFireForget()
    {
        settings.Strategy = ForgetStrategy.FireForget;
        return this;
    }
}
