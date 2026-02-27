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
/// Singleton registry that runs self-sustaining background polling loops for outbox message processing.
/// Supports multiple DbContext types, each with independent polling configuration.
/// Registered in EF Core's internal service provider and started lazily on the first <c>SaveChanges</c>
/// when outbox-enabled types exist in the model.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="OutboxPollRegistry"/>.
/// </remarks>
/// <param name="logger">Logger instance.</param>
internal sealed class OutboxPollRegistry(ILogger<OutboxPollRegistry> logger) : IDisposable
{
    private readonly ConcurrentDictionary<Type, DbContextPollingState> _states = new();
    private int disposed;

    // CLR reflection fallback cache — shared across ticks, thread-safe
    private static readonly ConcurrentDictionary<string, OutboxCommandExecutor> _deferedExecutionMethodByType = [];

    /// <summary>
    /// Holds per-DbContext polling state including the polling task, cancellation token source,
    /// configuration, and middleware specifier.
    /// </summary>
    private sealed class DbContextPollingState : IDisposable
    {
        public required IServiceProvider ApplicationServiceProvider { get; init; }
        public required Type DbContextType { get; init; }
        public required TimeSpan PollingInterval { get; init; }
        public required int MaxMessagesPerPoll { get; init; }
        public required OutboxCoordinationStrategy CoordinationStrategy { get; init; }
        public IMiddlewareSpecifier? MiddlewareSpecifier { get; set; }
        public CancellationTokenSource Cts { get; } = new();
        public Task? PollingTask { get; set; }

        public void Dispose()
        {
            Cts.Cancel();
            try
            {
                PollingTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Swallow — the task may have been cancelled or faulted during shutdown.
            }
            Cts.Dispose();
        }
    }

    /// <summary>
    /// Indicates whether polling has been started for at least one DbContext type.
    /// </summary>
    public bool Started => !_states.IsEmpty;

    /// <summary>
    /// Indicates whether polling has been started for the specified DbContext type.
    /// </summary>
    /// <param name="dbContextType">The concrete <see cref="DbContext"/> type to check.</param>
    /// <returns><c>true</c> if polling is active for the specified type; otherwise <c>false</c>.</returns>
    public bool IsStarted(Type dbContextType) => _states.ContainsKey(dbContextType);

    /// <summary>
    /// Performs thread-safe initialization of the polling loop for the given DbContext type.
    /// Each DbContext type gets its own independent polling loop with its own configuration.
    /// </summary>
    /// <param name="applicationServiceProvider">The application-level root service provider (for creating scopes).</param>
    /// <param name="model">The finalized EF Core model containing outbox annotations.</param>
    /// <param name="dbContextType">The concrete <see cref="DbContext"/> type to resolve per-tick.</param>
    public void EnsureStarted(IServiceProvider applicationServiceProvider, IModel model, Type dbContextType)
    {
        var coordinationStrategy = model.GetOutboxCoordinationStrategy();

        var state = new DbContextPollingState
        {
            ApplicationServiceProvider = applicationServiceProvider,
            DbContextType = dbContextType,
            PollingInterval = model.GetOutboxPollingInterval(),
            MaxMessagesPerPoll = model.GetOutboxMaxMessagesPerPoll(),
            CoordinationStrategy = coordinationStrategy,
        };

        if (!_states.TryAdd(dbContextType, state))
        {
            // Already registered — dispose the state we just created and return.
            state.Dispose();
            return;
        }

        DiscoverMiddlewareSpecifier(state);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "OutboxPollRegistry starting for {DbContextType}. PollingInterval={PollingInterval}, MaxMessagesPerPoll={MaxMessages}, Coordination={Coordination}",
                dbContextType.Name, state.PollingInterval, state.MaxMessagesPerPoll, coordinationStrategy);
        }

        state.PollingTask = Task.Run(() => RunAsync(state, state.Cts.Token));
    }

    /// <summary>
    /// Scans the entry assembly for a single <see cref="IMiddlewareSpecifier{T}"/> implementation
    /// matching the DbContext type in the given state and caches it.
    /// Runs exactly once during <see cref="EnsureStarted"/> per DbContext type; the resolved instance
    /// provides direct interface dispatch, eliminating per-message reflection overhead.
    /// Falls back to CLR reflection (via <see cref="LoadExecutor"/>) if no specifier is found.
    /// </summary>
    /// <param name="state">The polling state containing the DbContext type to match.</param>
    private void DiscoverMiddlewareSpecifier(DbContextPollingState state)
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
            {
                logger.LogInformation("Entry assembly not found; using CLR reflection fallback for outbox message dispatch.");
                return;
            }

            var specifierInterfaceType = typeof(IMiddlewareSpecifier<>).MakeGenericType(state.DbContextType);
            var specifierType = entryAssembly
                .GetTypes()
                .SingleOrDefault(t => t.IsAssignableTo(specifierInterfaceType));

            if (specifierType != null)
            {
                state.MiddlewareSpecifier = (IMiddlewareSpecifier)Activator.CreateInstance(specifierType)!;

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Found IMiddlewareSpecifier implementation: {SpecifierType}", specifierType.FullName);
                }
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("No IMiddlewareSpecifier found for {DbContextType}; using CLR reflection fallback.", state.DbContextType.Name);
                }
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("more than one"))
        {
            logger.LogError(ex, "Multiple IMiddlewareSpecifier implementations found for {DbContextType}; falling back to CLR reflection.", state.DbContextType.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error discovering IMiddlewareSpecifier for {DbContextType}; falling back to CLR reflection.", state.DbContextType.Name);
        }
    }

    private async Task RunAsync(DbContextPollingState state, CancellationToken ct)
    {
        // Yield immediately so the caller (EnsureStarted → Task.Run) is not blocked.
        await Task.Yield();

        using var timer = new PeriodicTimer(state.PollingInterval);

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
                await ExecuteTickAsync(state, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                logger.LogWarning("Application service provider disposed; stopping outbox polling loop for {DbContextType}.", state.DbContextType.Name);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception occurred while processing outbox for {DbContextType}.", state.DbContextType.Name);
            }
        }

        logger.LogDebug("Outbox polling loop exited for {DbContextType}.", state.DbContextType.Name);
    }

    private async Task ExecuteTickAsync(DbContextPollingState state, CancellationToken ct)
    {
        if (state.CoordinationStrategy == OutboxCoordinationStrategy.ExclusiveNode)
        {
            // ExclusiveNode is not yet fully implemented (see REV-013 deprecation)
            logger.LogWarning("ExclusiveNode coordination strategy is not yet implemented. Proceeding with SingleNode behavior for DbContext {DbContextType}.", state.DbContextType.Name);
        }

        using var scope = state.ApplicationServiceProvider.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(state.DbContextType);

        var outboxMessages = context.Set<OutboxMessage>();
        var query = outboxMessages.OrderBy(m => m.SequenceNumber);

        var outboxMessageArray = await query.Take(state.MaxMessagesPerPoll).ToArrayAsync(ct).ConfigureAwait(false);

        if (outboxMessageArray.Length == 0)
            return;

        var dbContextServiceProvider = context.GetInfrastructure();

        for (int i = 0; i < outboxMessageArray.Length; i++)
        {
            await ExecuteOutboxMessageAsync(outboxMessageArray[i], dbContextServiceProvider, state, ct).ConfigureAwait(false);
        }

        var producer = dbContextServiceProvider.GetRequiredService<IProducer>();
        producer.Flush(ct);

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static ValueTask ExecuteOutboxMessageAsync(OutboxMessage msg, IServiceProvider sp, DbContextPollingState state, CancellationToken ct)
    {
        if (state.MiddlewareSpecifier is { } spec && spec.CanHandle(msg))
            return spec.ExecuteAsync(msg, sp, ct);

        return InvokeWithClr(msg, sp, ct);
    }

    private static ValueTask InvokeWithClr(OutboxMessage msg, IServiceProvider sp, CancellationToken ct)
    {
        return _deferedExecutionMethodByType.GetOrAdd(msg.Type, static key => LoadExecutor(key)).Invoke(msg, sp, ct);
    }

    private static OutboxCommandExecutor LoadExecutor(string assemblyQualifiedName)
    {
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
    /// Cancels all polling loops and waits for graceful shutdown.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref this.disposed, 1, 0) != 0)
            return;

        foreach (var kvp in _states)
        {
            kvp.Value.Dispose();
        }

        _states.Clear();
    }
}
