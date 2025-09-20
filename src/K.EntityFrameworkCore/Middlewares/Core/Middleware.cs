using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal abstract class Middleware<T>(MiddlewareSettings<T> settings) : IMiddleware<T>
    where T : class
{
    private IMiddleware<T>? next;

    protected Middleware() : this(new MiddlewareSettings<T>(isMiddlewareEnabled: true)) { }

    /// <summary>
    /// Gets a value indicating whether this middleware is enabled based on the settings.
    /// </summary>
    public bool IsEnabled => settings.IsMiddlewareEnabled;

    IMiddleware<T>? IMiddleware<T>.Next
    {
        get => this.next;
        set => this.next = value;
    }

    public virtual ValueTask<T?> InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        return this.next?.InvokeAsync(envelope, cancellationToken) ?? ValueTask.FromResult((T?)envelope.Message);
    }
}
