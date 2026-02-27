using K.EntityFrameworkCore.Middlewares.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace K.EntityFrameworkCore.Extensions;

internal class KafkaMiddlewareInterceptor : SaveChangesInterceptor
{
    /// <remarks>
    /// This synchronous override uses Confluent Kafka's synchronous <c>Produce</c> method
    /// with a delivery-error handler. It requires all producer types to be configured with
    /// <c>ForgetStrategy.FireForget</c> via <c>.HasForget(f => f.UseFireForget())</c>.
    /// If a producer is not configured for fire-and-forget, an <see cref="InvalidOperationException"/>
    /// is thrown. Prefer <see cref="SavingChangesAsync"/> for full delivery guarantees.
    /// </remarks>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        var dbContext = eventData.Context;
        if (dbContext == null)
        {
            return base.SavingChanges(eventData, result);
        }

        IServiceProvider serviceProvider = dbContext.GetInfrastructure();

        var scopedCommandRegistry = serviceProvider.GetRequiredService<ScopedCommandRegistry>();

        scopedCommandRegistry.Execute(serviceProvider);

        result = base.SavingChanges(eventData, result);

        TryStartOutboxWorker(dbContext, serviceProvider);

        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext == null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        IServiceProvider serviceProvider = dbContext.GetInfrastructure();

        var scopedCommandRegistry = serviceProvider.GetRequiredService<ScopedCommandRegistry>();

        await scopedCommandRegistry.ExecuteAsync(serviceProvider, cancellationToken);

        result = await base.SavingChangesAsync(eventData, result, cancellationToken);

        TryStartOutboxWorker(dbContext, serviceProvider);

        return result;
    }

    private static void TryStartOutboxWorker(DbContext dbContext, IServiceProvider serviceProvider)
    {
        var outboxRegistry = serviceProvider.GetService<OutboxPollRegistry>();
        if (outboxRegistry == null || outboxRegistry.IsStarted(dbContext.GetType()))
            return;

        var model = dbContext.Model;
        bool hasOutbox = model.FindEntityType(typeof(OutboxMessage)) != null;

        if (!hasOutbox)
            return;

        var options = dbContext.GetService<IDbContextOptions>();
        var coreOptions = options.FindExtension<CoreOptionsExtension>();
        var appServiceProvider = coreOptions?.ApplicationServiceProvider;

        if (appServiceProvider == null)
            return;

        try
        {
            outboxRegistry.EnsureStarted(appServiceProvider, model, dbContext.GetType());
        }
        catch (Exception ex)
        {
            var logger = appServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger<KafkaMiddlewareInterceptor>();
            logger.LogError(ex, "Failed to start outbox polling for DbContext {DbContextType}.",
                dbContext.GetType().Name);
            throw;
        }
    }
}
