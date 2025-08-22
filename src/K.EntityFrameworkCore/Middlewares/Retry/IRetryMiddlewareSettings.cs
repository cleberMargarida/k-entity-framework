
namespace K.EntityFrameworkCore.Middlewares.Retry
{
    /// <summary>
    /// Defines the configuration interface for retry middleware settings.
    /// </summary>
    public interface IRetryMiddlewareSettings
    {
        /// <summary>
        /// The max backoff time in milliseconds before retrying, this
        /// is the atmost backoff allowed for exponentially backed off requests. 
        /// </summary>
        int RetryBackoffMaxMilliseconds { get; }

        /// <summary>
        /// The backoff time in milliseconds before retrying a protocol request, this is
        /// the first backoff time, and will be backed off exponentially until number of
        /// retries is exhausted.
        /// </summary>
        int RetryBackoffMilliseconds { get; }

        /// <summary>
        /// How many times to retry sending a failing Message.
        /// </summary>
        int MaxRetries { get; }
    }
}