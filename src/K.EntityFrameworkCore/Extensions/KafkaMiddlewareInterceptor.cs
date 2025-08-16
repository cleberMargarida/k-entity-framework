using Microsoft.EntityFrameworkCore.Diagnostics;

namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaMiddlewareInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            // Here you can intercept the saving changes event asynchronously
            // and perform any custom logic before the changes are saved to the database.
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }
}
