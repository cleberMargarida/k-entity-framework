using K.EntityFrameworkCore.MiddlewareOptions;
using Microsoft.Extensions.Logging;

namespace K.EntityFrameworkCore.Middlewares;

internal abstract class BatchMiddleware<T> : Middleware<T>
    where T : class
{
#if NET9_0_OR_GREATER
    private static readonly Lock sync = new();
#else
    private static readonly object sync = new();
#endif
    private static readonly List<Envelope<T>> currentBatch = [];
    private static Timer? flushTimer;
    private static WeakReference<BatchMiddleware<T>>? instanceRef;

    private readonly BatchMiddlewareOptions<T> options;

    protected BatchMiddleware(BatchMiddlewareOptions<T> options) : base(options)
    {
        instanceRef = new WeakReference<BatchMiddleware<T>>(this);

        if (options.BatchTimeout > TimeSpan.Zero)
        {
            flushTimer ??= new Timer(TimerCallback, null, options.BatchTimeout, options.BatchTimeout);
        }

        this.options = options;
    }

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        List<Envelope<T>>? batchToSend = null;

        lock (sync)
        {
            if (envelope is not null)
            {
                currentBatch.Add(envelope);
            }

            bool sizeLimitReached = options!.BatchSize > 0 && currentBatch.Count >= options.BatchSize;
            bool timerTriggered = envelope is null && currentBatch.Count > 0;

            if (sizeLimitReached || timerTriggered)
            {
                batchToSend = new(currentBatch);
                currentBatch.Clear();
            }

            if (batchToSend is null or { Count: 0 })
            {
                return;
            }
        }

        try
        {
            await InvokeAsync(batchToSend, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (sync)
            {
                var currentBatchSnapshot = currentBatch.ToArray();
                currentBatch.Clear();

                foreach (var item in batchToSend)
                {
                    currentBatch.Add(item);
                }

                foreach (var item in currentBatchSnapshot)
                {
                    currentBatch.Add(item);
                }
            }

            throw;
        }
    }

    protected abstract Task InvokeAsync(ICollection<Envelope<T>> batchToSend, CancellationToken cancellationToken);

    private static void TimerCallback(object? _)
    {
        _ = TimerCallbackAsync();
    }

    private static async Task TimerCallbackAsync()
    {
        if (instanceRef?.TryGetTarget(out BatchMiddleware<T>? middleware) != true || middleware is null)
        {
            return;
        }

        await middleware.InvokeAsync(currentBatch, CancellationToken.None);
    }
}
