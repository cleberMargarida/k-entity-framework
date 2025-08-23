using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring OutboxMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class OutboxBuilder<T>(OutboxMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Configures immediate publishing strategy with fallback to background processing.
    /// Messages are published immediately after saving. If successful, they are removed.
    /// If immediate publishing fails, messages fall back to background processing.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> UseImmediateWithFallback()
    {
        settings.Strategy = OutboxPublishingStrategy.ImmediateWithFallback;
        return this;
    }

    /// <summary>
    /// Configures background-only publishing strategy.
    /// Messages are always published in the background after saving.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> UseBackgroundOnly()
    {
        settings.Strategy = OutboxPublishingStrategy.BackgroundOnly;
        return this;
    }
}







/// <summary>
/// Fluent builder for configuring producer ForgetMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerForgetBuilder<T>(ProducerForgetMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Sets the forget strategy to AwaitForget.
    /// </summary>
    /// <param name="timeout">
    /// The timeout duration for awaiting message processing.
    /// </param>
    /// <returns>The builder instance.</returns>
    public ProducerForgetBuilder<T> UseAwaitForget(TimeSpan? timeout = null)
    {
        settings.Strategy = ForgetStrategy.AwaitForget;
        settings.Timeout = timeout ?? TimeSpan.FromSeconds(30);
        return this;
    }

    /// <summary>
    /// Sets the forget strategy to FireForget.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public ProducerForgetBuilder<T> UseFireForget()
    {
        settings.Strategy = ForgetStrategy.FireForget;
        return this;
    }
}
