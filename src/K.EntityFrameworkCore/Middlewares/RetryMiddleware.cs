namespace K.EntityFrameworkCore.Middlewares;

internal abstract class RetryMiddleware<T>(RetryMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    private static readonly Random random = new();

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= settings.MaxRetryAttempts; attempt++)
        {
            try
            {
                await base.InvokeAsync(envelope, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < settings.MaxRetryAttempts && ShouldRetry(ex))
            {
                lastException = ex;
                
                if (attempt < settings.MaxRetryAttempts)
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
        if (settings.ShouldRetryPredicate != null)
        {
            return settings.ShouldRetryPredicate(exception);
        }

        if (settings.RetriableExceptionTypes != null && settings.RetriableExceptionTypes.Length > 0)
        {
            return settings.RetriableExceptionTypes.Contains(exception.GetType());
        }

        return true;
    }

    private TimeSpan CalculateDelay(int attemptNumber)
    {
        TimeSpan delay = settings.BackoffStrategy switch
        {
            RetryBackoffStrategy.Fixed => settings.BaseDelay,
            RetryBackoffStrategy.Linear => TimeSpan.FromMilliseconds(settings.BaseDelay.TotalMilliseconds * attemptNumber),
            RetryBackoffStrategy.Exponential => TimeSpan.FromMilliseconds(
                settings.BaseDelay.TotalMilliseconds * Math.Pow(settings.BackoffMultiplier, attemptNumber - 1)),
            _ => settings.BaseDelay
        };

        if (delay > settings.MaxDelay)
        {
            delay = settings.MaxDelay;
        }

        if (settings.UseJitter)
        {
            // Add random jitter up to 20% of the delay
            var jitterAmount = delay.TotalMilliseconds * 0.2 * random.NextDouble();
            delay = delay.Add(TimeSpan.FromMilliseconds(jitterAmount));
        }

        return delay;
    }
}
