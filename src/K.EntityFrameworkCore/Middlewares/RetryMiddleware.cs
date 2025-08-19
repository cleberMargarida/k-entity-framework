using K.EntityFrameworkCore.MiddlewareOptions;

namespace K.EntityFrameworkCore.Middlewares;

internal abstract class RetryMiddleware<T>(RetryMiddlewareOptions<T> options) : Middleware<T>(options)
    where T : class
{
    private static readonly Random random = new();

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= options.MaxRetryAttempts; attempt++)
        {
            try
            {
                await base.InvokeAsync(envelope, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < options.MaxRetryAttempts && ShouldRetry(ex))
            {
                lastException = ex;
                
                if (attempt < options.MaxRetryAttempts)
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

    private bool ShouldRetry(Exception exception)
    {
        if (options.ShouldRetryPredicate != null)
        {
            return options.ShouldRetryPredicate(exception);
        }

        if (options.RetriableExceptionTypes != null && options.RetriableExceptionTypes.Length > 0)
        {
            return options.RetriableExceptionTypes.Contains(exception.GetType());
        }

        return true;
    }

    private TimeSpan CalculateDelay(int attemptNumber)
    {
        TimeSpan delay = options.BackoffStrategy switch
        {
            RetryBackoffStrategy.Fixed => options.BaseDelay,
            RetryBackoffStrategy.Linear => TimeSpan.FromMilliseconds(options.BaseDelay.TotalMilliseconds * attemptNumber),
            RetryBackoffStrategy.Exponential => TimeSpan.FromMilliseconds(
                options.BaseDelay.TotalMilliseconds * Math.Pow(options.BackoffMultiplier, attemptNumber - 1)),
            _ => options.BaseDelay
        };

        if (delay > options.MaxDelay)
        {
            delay = options.MaxDelay;
        }

        if (options.UseJitter)
        {
            // Add random jitter up to 20% of the delay
            var jitterAmount = delay.TotalMilliseconds * 0.2 * random.NextDouble();
            delay = delay.Add(TimeSpan.FromMilliseconds(jitterAmount));
        }

        return delay;
    }
}
