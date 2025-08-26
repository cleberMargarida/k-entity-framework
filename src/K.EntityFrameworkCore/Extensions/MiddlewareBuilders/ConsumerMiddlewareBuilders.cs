using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Inbox;
using System.Linq.Expressions;

namespace K.EntityFrameworkCore.Extensions.MiddlewareBuilders;

/// <summary>
/// Fluent builder for configuring InboxMiddleware settings.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class InboxBuilder<T>(InboxMiddlewareSettings<T> settings) where T : class
{
    /// <summary>
    /// Configures the deduplication strategy by specifying which properties or values to use for duplicate detection.
    /// Uses expression trees to compile a fast accessor that extracts values for hashing.
    /// </summary>
    /// <param name="valueAccessor">Expression that extracts the value(s) to use for deduplication.</param>
    /// <returns>The builder instance.</returns>
    public InboxBuilder<T> HasDeduplicateProperties(Expression<Func<T, object>> valueAccessor)
    {
        settings.DeduplicationValueAccessor = valueAccessor;
        return this;
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
