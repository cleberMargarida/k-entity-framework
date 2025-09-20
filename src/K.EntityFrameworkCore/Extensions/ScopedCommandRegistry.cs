using K.EntityFrameworkCore.Middlewares.Producer;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Extensions;

internal class ScopedCommandRegistry
{
    private readonly Queue<ScopedCommand> commands = new(3);

    public void Add(ScopedCommand command)
    {
        this.commands.Enqueue(command);
    }

    public async ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        while (this.commands.TryDequeue(out var command))
            await command.Invoke(serviceProvider, cancellationToken);
    }
}

internal readonly struct ProducerMiddlewareInvokeCommand<T>(T message)
    where T : class
{
    public async ValueTask ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await serviceProvider.GetRequiredService<ProducerMiddlewareInvoker<T>>().InvokeAsync(new Envelope<T>(message), cancellationToken);
    }
}


/// <summary>
/// Represents an asynchronous operation executed within the service scope lifecycle,
/// tied to the current DbContext.
/// </summary>
public delegate ValueTask ScopedCommand(IServiceProvider serviceProvider, CancellationToken cancellationToken);
