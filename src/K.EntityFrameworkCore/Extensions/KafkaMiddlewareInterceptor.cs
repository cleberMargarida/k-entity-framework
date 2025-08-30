using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaMiddlewareInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var dbContext = eventData.Context;
            if (dbContext == null)
            {
                return base.SavingChanges(eventData, result);
            }

            IServiceProvider serviceProvider = dbContext.GetInfrastructure();

            // first database operation result
            result = base.SavingChanges(eventData, result);

            // second kafka operation
            serviceProvider.GetRequiredService<ScopedCommandRegistry>().ExecuteAsync(serviceProvider).AsTask().GetAwaiter().GetResult();

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

            // first database operation result
            result = await base.SavingChangesAsync(eventData, result, cancellationToken);

            // second kafka operation
            await serviceProvider.GetRequiredService<ScopedCommandRegistry>().ExecuteAsync(serviceProvider, cancellationToken);

            return result;
        }
    }
}
