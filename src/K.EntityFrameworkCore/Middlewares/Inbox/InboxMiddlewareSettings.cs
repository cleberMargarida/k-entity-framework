using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Producer;
using Microsoft.EntityFrameworkCore.Metadata;
using System.IO.Hashing;
using System.Linq.Expressions;
using System.Text;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

/// <summary>
/// Configuration options for the InboxMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
/// <remarks>
/// Initializes a new instance of the InboxMiddlewareSettings class.
/// </remarks>
public class InboxMiddlewareSettings<T>(IModel model) : MiddlewareSettings<T>(model.IsInboxEnabled<T>())
    where T : class
{
    private static readonly byte[] TypeSalt = Encoding.UTF8.GetBytes(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>
    /// Gets the timeout for duplicate message detection.
    /// Messages older than this timeout will be considered safe to process again.
    /// Default is 24 hours.
    /// </summary>
    public TimeSpan DeduplicationTimeWindow => model.GetInboxDeduplicationTimeWindow<T>() ?? TimeSpan.FromHours(24);


    /// <summary>
    /// Gets the compiled deduplication value accessor from model annotations.
    /// </summary>
    private Func<T, object>? DeduplicationValueAccessor
    {
        get
        {
            if (field != null)
                return field;

            var valueAccessorExpression = model.GetInboxDeduplicationValueAccessor<T>();

            if (valueAccessorExpression == null)
                return null;

            var parameter = Expression.Parameter(typeof(T), "message");

            Expression propertyExpression;
            if (valueAccessorExpression is LambdaExpression lambda)
            {
                var parameterReplacer = new ParameterReplacer(lambda.Parameters[0], parameter);
                propertyExpression = parameterReplacer.Visit(lambda.Body);
            }
            else
            {
                propertyExpression = valueAccessorExpression;
            }

            var lambdaExpression = Expression.Lambda<Func<T, object>>(propertyExpression, parameter);
            field = lambdaExpression.Compile();

            return field;
        }

        set;
    }

    internal ulong Hash(T message)
    {
        var accessor = DeduplicationValueAccessor
            ?? throw new InvalidOperationException(
                $"No deduplication value accessor has been configured for type '{typeof(T).Name}'. " +
                $"Use HasDeduplicateProperties() to specify which properties should be used for duplicate detection.");

        var value = accessor(message);
        var jsonValue = value?.ToString() ?? "null";

        // Calculate required byte counts without allocating
        int valueByteCount = Encoding.UTF8.GetByteCount(jsonValue);
        int totalLength = TypeSalt.Length + valueByteCount;

        // Use stackalloc for small buffers, rent from pool for large ones
        const int StackAllocThreshold = 512;
        Span<byte> saltedData = totalLength <= StackAllocThreshold
            ? stackalloc byte[totalLength]
            : new byte[totalLength]; // fallback for abnormally large keys

        // Copy salt prefix, then encode value directly into the span
        TypeSalt.CopyTo(saltedData);
        Encoding.UTF8.GetBytes(jsonValue.AsSpan(), saltedData.Slice(TypeSalt.Length));

        return XxHash64.HashToUInt64(saltedData);
    }
}
