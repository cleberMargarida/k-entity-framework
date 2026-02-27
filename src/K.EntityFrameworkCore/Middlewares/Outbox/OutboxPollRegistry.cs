using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace K.EntityFrameworkCore.Middlewares.Outbox;

internal delegate ValueTask OutboxCommandExecutor(OutboxMessage msg, IServiceProvider sp, CancellationToken ct);

/// <summary>
/// Singleton registry that runs a self-sustaining background polling loop for outbox message processing.
/// Registered in EF Core's internal service provider and started lazily on the first <c>SaveChanges</c>
/// when outbox-enabled types exist in the model.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="OutboxPollRegistry"/>.
/// </remarks>
/// <param name="logger">Logger instance.</param>
internal sealed class OutboxPollRegistry(ILogger<OutboxPollRegistry> logger) : IDisposable
{
    private readonly CancellationTokenSource cts = new();
    private Task? pollingTask;
    private int started;
    private int disposed;

    // Captured during EnsureStarted — used by the polling loop
    private IServiceProvider? applicationServiceProvider;
    private Type? dbContextType;
    private TimeSpan pollingInterval;
    private int maxMessagesPerPoll;

    /// <summary>
    /// Pre-resolved middleware specifier discovered once at startup via assembly scan.
    /// When non-null, provides zero-allocation interface dispatch for outbox message processing,
    /// bypassing the CLR reflection fallback path.
    /// </summary>
    private IMiddlewareSpecifier? middlewareSpecifier;

    // CLR reflection fallback cache — shared across ticks, thread-safe
    private static readonly ConcurrentDictionary<string, OutboxCommandExecutor> _deferedExecutionMethodByType = [];

    /// <summary>
    /// Indicates whether the polling loop has been started.
    /// </summary>
    public bool Started => Volatile.Read(ref this.started) == 1;

    /// <summary>
    /// Performs thread-safe once-only initialization of the polling loop.
    /// </summary>
    /// <param name="applicationServiceProvider">The application-level root service provider (for creating scopes).</param>
    /// <param name="model">The finalized EF Core model containing outbox annotations.</param>
    /// <param name="dbContextType">The concrete <see cref="DbContext"/> type to resolve per-tick.</param>
    public void EnsureStarted(IServiceProvider applicationServiceProvider, IModel model, Type dbContextType)
    {
        if (Interlocked.CompareExchange(ref this.started, 1, 0) != 0)
            return;

        this.pollingInterval = model.GetOutboxPollingInterval();
        this.maxMessagesPerPoll = model.GetOutboxMaxMessagesPerPoll();
        var coordinationStrategy = model.GetOutboxCoordinationStrategy();

        this.applicationServiceProvider = applicationServiceProvider;
        this.dbContextType = dbContextType;

        DiscoverMiddlewareSpecifier(dbContextType);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
            "OutboxPollRegistry starting for {DbContextType}. PollingInterval={PollingInterval}, MaxMessagesPerPoll={MaxMessages}, Coordination={Coordination}",
            dbContextType.Name, this.pollingInterval, this.maxMessagesPerPoll, coordinationStrategy);
        }


        this.pollingTask = Task.Run(() => RunAsync(this.cts.Token));
    }

    /// <summary>
    /// Scans the entry assembly for a single <see cref="IMiddlewareSpecifier{T}"/> implementation
    /// matching <paramref name="dbContextType"/> and caches it in <see cref="middlewareSpecifier"/>.
    /// Runs exactly once during <see cref="EnsureStarted"/>; the resolved instance provides
    /// direct interface dispatch, eliminating per-message reflection overhead.
    /// Falls back to CLR reflection (via <see cref="LoadExecutor"/>) if no specifier is found.
    /// </summary>
    /// <param name="dbContextType">The concrete <see cref="DbContext"/> type to match.</param>
    private void DiscoverMiddlewareSpecifier(Type dbContextType)
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
            {
                logger.LogInformation("Entry assembly not found; using CLR reflection fallback for outbox message dispatch.");
                return;
            }

            var specifierInterfaceType = typeof(IMiddlewareSpecifier<>).MakeGenericType(dbContextType);
            var specifierType = entryAssembly
                .GetTypes()
                .SingleOrDefault(t => t.IsAssignableTo(specifierInterfaceType));

            if (specifierType != null)
            {
                this.middlewareSpecifier = (IMiddlewareSpecifier)Activator.CreateInstance(specifierType)!;

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Found IMiddlewareSpecifier implementation: {SpecifierType}", specifierType.FullName);
                }

            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("No IMiddlewareSpecifier found for {DbContextType}; using CLR reflection fallback.", dbContextType.Name);
                }
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("more than one"))
        {
            logger.LogError(ex, "Multiple IMiddlewareSpecifier implementations found for {DbContextType}; falling back to CLR reflection.", dbContextType.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error discovering IMiddlewareSpecifier for {DbContextType}; falling back to CLR reflection.", dbContextType.Name);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        // Yield immediately so the caller (EnsureStarted → Task.Run) is not blocked.
        await Task.Yield();

        using var timer = new PeriodicTimer(this.pollingInterval);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                    break;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ExecuteTickAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                logger.LogWarning("Application service provider disposed; stopping outbox polling loop.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception occurred while processing outbox.");
            }
        }

        logger.LogDebug("Outbox polling loop exited.");
    }

    private async Task ExecuteTickAsync(CancellationToken ct)
    {
        using var scope = this.applicationServiceProvider!.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(this.dbContextType!);

        var outboxMessages = context.Set<OutboxMessage>();
        var query = outboxMessages.OrderBy(m => m.SequenceNumber);

        var outboxMessageArray = await query.Take(this.maxMessagesPerPoll).ToArrayAsync(ct).ConfigureAwait(false);

        if (outboxMessageArray.Length == 0)
            return;

        var dbContextServiceProvider = context.GetInfrastructure();
        var outboxFinishedTasks = new Task[outboxMessageArray.Length];

        for (int i = 0; i < outboxMessageArray.Length; i++)
        {
            outboxFinishedTasks[i] = ExecuteOutboxMessageAsync(outboxMessageArray[i], dbContextServiceProvider, ct).AsTask();
        }

        var producer = dbContextServiceProvider.GetRequiredService<IProducer>();
        producer.Flush(ct);

        await Task.WhenAll(outboxFinishedTasks).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private ValueTask ExecuteOutboxMessageAsync(OutboxMessage msg, IServiceProvider sp, CancellationToken ct)
    {
        if (this.middlewareSpecifier is { } spec && spec.CanHandle(msg))
            return spec.ExecuteAsync(msg, sp, ct);

        return InvokeWithClr(msg, sp, ct);
    }

    private ValueTask InvokeWithClr(OutboxMessage msg, IServiceProvider sp, CancellationToken ct)
    {
        return _deferedExecutionMethodByType.GetOrAdd(msg.Type, LoadExecutor).Invoke(msg, sp, ct);
    }

    private OutboxCommandExecutor LoadExecutor(string assemblyQualifiedName)
    {
        logger.LogWarning("Source generator not found for type '{MessageType}'. Falling back to CLR reflection strategy. " +
                          "Consider adding package 'K.EntityFrameworkCore.CodeGen' for better performance.", assemblyQualifiedName);

        var msgParam = Expression.Parameter(typeof(OutboxMessage), "msg");
        var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var method = typeof(OutboxPollRegistry)
            .GetMethod(nameof(InvokeMiddleware), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(Type.GetType(assemblyQualifiedName, true)!);

        var body = Expression.Call(null, method, msgParam, spParam, ctParam);

        return Expression.Lambda<OutboxCommandExecutor>(body, msgParam, spParam, ctParam).Compile();
    }

    /// <summary>
    /// Processes an outbox message by invoking the producer middleware pipeline directly.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="outboxMessage">The outbox message to process.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public static async ValueTask InvokeMiddleware<T>(OutboxMessage outboxMessage, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        where T : class
    {
        outboxMessage.TypeLoaded = typeof(T);

        var middleware = serviceProvider.GetRequiredService<OutboxProducerMiddleware<T>>();
        var envelope = new Envelope<T>();

        envelope.WeakReference.SetTarget(outboxMessage);

        await middleware.InvokeAsync(envelope, cancellationToken);
    }

    /// <summary>
    /// Cancels the polling loop and waits for graceful shutdown.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref this.disposed, 1, 0) != 0)
            return;

        this.cts.Cancel();
        try
        {
            this.pollingTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Swallow — the task may have been cancelled or faulted during shutdown.
        }
        this.cts.Dispose();
    }
}
