using K.EntityFrameworkCore.Middlewares.Producer;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Extensions;

[ScopedService]
internal class ScopedCommandRegistry
{
    private readonly Queue<ScopedCommand> commands = new(3);

    public void Add(ScopedCommand command)
    {
        commands.Enqueue(command);
    }

    public async ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        while (commands.TryDequeue(out var command))
            await command.Invoke(serviceProvider, cancellationToken);
    }
}

internal readonly struct ProducerMiddlewareInvokeCommand<T>(T message)
    where T : class
{
    public ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return serviceProvider.GetRequiredService<ProducerMiddlewareInvoker<T>>().InvokeAsync(message.Seal(), cancellationToken);
    }
}


/// <summary>
/// Represents an asynchronous operation executed within the service scope lifecycle,
/// tied to the current DbContext.
/// </summary>
public delegate ValueTask ScopedCommand(IServiceProvider serviceProvider, CancellationToken cancellationToken);
