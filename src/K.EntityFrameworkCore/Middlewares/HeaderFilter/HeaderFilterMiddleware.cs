using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.HeaderFilter;

/// <summary>
/// Middleware that filters messages based on header values using predicate expressions.
/// This middleware runs after deserialization and can filter messages before they reach the inbox or business logic.
/// </summary>
/// <typeparam name="T">The message type being processed.</typeparam>
[ScopedService]
internal class HeaderFilterMiddleware<T>(HeaderFilterMiddlewareSettings<T> headerSetting) : Middleware<T>(headerSetting)
    where T : class
{
    public override ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        if (IsMatch(envelope.Headers))
        {
            return base.InvokeAsync(envelope, cancellationToken);
        }

        return ValueTask.FromResult<T?>(null);
    }

    public bool IsMatch(IReadOnlyDictionary<string, string> headers)
    {
        foreach (var filter in headerSetting.HeaderFilters)
        {
            string expectedHeaderKey = filter.Key;
            string expectedHeaderValue = filter.Value;

            if (headers.TryGetValue(expectedHeaderKey, out var actualHeaderValue) &&
                string.Equals(actualHeaderValue, expectedHeaderValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
