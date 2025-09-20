using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// DbContext extensions messages.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Produces a message of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbContext"></param>
    /// <param name="message"></param>
    public static void Produce<T>(this DbContext dbContext, T message)
        where T : class
    {
        var serviceProvider = dbContext.GetInfrastructure();
        var commandRegistry = serviceProvider.GetRequiredService<ScopedCommandRegistry>();

        commandRegistry.Add(new ProducerMiddlewareInvokeCommand<T>(message).ExecuteAsync);
    }

    /// <summary>
    /// Gets a topic for producing and consuming messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    public static Topic<T> Topic<T>(this DbContext dbContext)
        where T : class
    {
        return new Topic<T>(dbContext);
    }
}
