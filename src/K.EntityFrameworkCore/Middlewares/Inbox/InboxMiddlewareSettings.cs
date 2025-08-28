using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;
using K.EntityFrameworkCore.Middlewares.Producer;
using System.IO.Hashing;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace K.EntityFrameworkCore.Middlewares.Inbox;

/// <summary>
/// Configuration options for the InboxMiddleware.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
[SingletonService]
public class InboxMiddlewareSettings<T> : MiddlewareSettings<T>
    where T : class
{
    private Func<T, object>? deduplicationValueAccessor;
    private static readonly byte[] TypeSalt = Encoding.UTF8.GetBytes(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>
    /// Gets or sets the timeout for duplicate message detection.
    /// Messages older than this timeout will be considered safe to process again.
    /// Default is 24 hours.
    /// </summary>
    public TimeSpan DeduplicationTimeWindow { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the interval for automatic cleanup operations.
    /// Default is 1 hour.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the expression used to extract values for deduplication.
    /// </summary>
    public Expression<Func<T, object>>? DeduplicationValueAccessor
    {
        set
        {
            if (value == null)
            {
                deduplicationValueAccessor = null;
                return;
            }

            var parameter = Expression.Parameter(typeof(T), "message");

            Expression propertyExpression;
            if (value is LambdaExpression lambda)
            {
                var parameterReplacer = new ParameterReplacer(lambda.Parameters[0], parameter);
                propertyExpression = parameterReplacer.Visit(lambda.Body);
            }
            else
            {
                propertyExpression = value;
            }

            var lambdaExpression = Expression.Lambda<Func<T, object>>(propertyExpression, parameter);
            deduplicationValueAccessor = lambdaExpression.Compile();
        }
    }

    /// <summary>
    /// Computes a hash for the envelope using xxHash64 algorithm for fast deduplication.
    /// Includes type-specific salt to ensure type isolation in hash values.
    /// </summary>
    /// <param name="envelope">The envelope to compute hash for.</param>
    /// <returns>A 64-bit hash value that includes type-specific salt.</returns>
    internal ulong Hash(Envelope<T> envelope)
    {
        if (envelope.Message == null)
        {
            throw new InvalidOperationException("Cannot compute hash for envelope with null message.");
        }

        byte[] dataToHash;

        if (deduplicationValueAccessor != null)
        {
            var value = deduplicationValueAccessor(envelope.Message);
            var jsonValue = JsonSerializer.Serialize(value);
            dataToHash = Encoding.UTF8.GetBytes(jsonValue);
        }
        else
        {
            throw new InvalidOperationException(
                $"No deduplication value accessor has been configured for type '{typeof(T).Name}'. " +
                $"Use HasDeduplicateProperties() to specify which properties should be used for duplicate detection.");
        }

        byte[] saltedData = new byte[dataToHash.Length + TypeSalt.Length];
        Array.Copy(TypeSalt, 0, saltedData, 0, TypeSalt.Length);
        Array.Copy(dataToHash, 0, saltedData, TypeSalt.Length, dataToHash.Length);

        var hashBytes = XxHash64.Hash(saltedData);
        
        return BitConverter.ToUInt64(hashBytes, 0);
    }
}
