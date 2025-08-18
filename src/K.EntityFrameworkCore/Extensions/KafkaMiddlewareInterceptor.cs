using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Confluent.Kafka;

namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaMiddlewareInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var dbContext = eventData.Context;
            if (dbContext == null)
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            IServiceProvider serviceProvider = dbContext.GetInfrastructure();

            var queue = serviceProvider.GetRequiredService<EventProcessingQueue>();
            while (queue.Dequeue(out var operationDelegate))
            {
                await operationDelegate(serviceProvider, cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
