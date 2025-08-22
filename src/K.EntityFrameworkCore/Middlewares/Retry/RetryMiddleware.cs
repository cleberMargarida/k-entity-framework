using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Retry;

internal abstract class RetryMiddleware<T>(RetryMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;
        int maxRetries = settings.MaxRetries;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await base.InvokeAsync(envelope, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    var delay = CalculateDelay(attempt + 1);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        if (lastException != null)
        {
            throw lastException;
        }
    }

    private TimeSpan CalculateDelay(int attemptNumber)
    {
        int baseDelayMs = settings.RetryBackoffMilliseconds;
        int maxDelayMs = settings.RetryBackoffMaxMilliseconds;
        double delayMs = baseDelayMs * Math.Pow(2, attemptNumber - 1);
        
        if (delayMs > maxDelayMs)
        {
            delayMs = maxDelayMs;
        }
        
        return TimeSpan.FromMilliseconds(delayMs);
    }
}
