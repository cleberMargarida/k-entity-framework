using K.EntityFrameworkCore.Middlewares;
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
    /// Publishes a message of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbContext"></param>
    /// <param name="message"></param>
    public static void Publish<T>(this DbContext dbContext, T message)
        where T : class
    {
        var serviceProvider = dbContext.GetInfrastructure();

        var commandRegistry = serviceProvider.GetRequiredService<ScopedCommandRegistry>();

        commandRegistry.Add(new PublishCommand<T>(message).ExecuteAsync);
    }
}

internal class ScopedCommandRegistry
{
    private readonly List<ScopedCommand> commands = new(3);

    public void Add(ScopedCommand command)
    {
        commands.Add(command);
    }

    public async ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        foreach (var command in commands)
        {
            await command.Invoke(serviceProvider, cancellationToken);
        }
    }
}

internal readonly struct PublishCommand<T>(T message)
    where T : class
{
    public ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return serviceProvider.GetRequiredService<ProducerMiddlewareInvoker<T>>().InvokeAsync(new Envelope<T>(message), cancellationToken);
    }
}


/// <summary>
/// Represents an asynchronous operation executed within the service scope lifecycle,
/// tied to the current DbContext.
/// </summary>
public delegate ValueTask ScopedCommand(IServiceProvider serviceProvider, CancellationToken cancellationToken);
