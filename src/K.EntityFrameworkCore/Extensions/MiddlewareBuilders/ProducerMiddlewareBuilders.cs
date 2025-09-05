using K.EntityFrameworkCore.Middlewares.Forget;
using K.EntityFrameworkCore.Middlewares.Outbox;
using Microsoft.EntityFrameworkCore.Metadata;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring OutboxMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class OutboxBuilder<T>(IMutableModel model) where T : class
{
    /// <summary>
    /// Configures immediate producing strategy with fallback to background processing.
    /// Messages are produced immediately after saving. If successful, they are removed.
    /// If immediate producing fails, messages fall back to background processing.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> UseImmediateWithFallback()
    {
        model.SetOutboxPublishingStrategy<T>(OutboxPublishingStrategy.ImmediateWithFallback);
        return this;
    }

    /// <summary>
    /// Configures background-only producing strategy.
    /// Messages are always produced in the background after saving.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public OutboxBuilder<T> UseBackgroundOnly()
    {
        model.SetOutboxPublishingStrategy<T>(OutboxPublishingStrategy.BackgroundOnly);
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
