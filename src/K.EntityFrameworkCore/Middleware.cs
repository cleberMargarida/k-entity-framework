using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace K.EntityFrameworkCore;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal class ScopedServiceAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal class SingletonServiceAttribute : Attribute;

[ScopedService]
internal class Middleware<T> : IMiddlewareConfig<T>, IMiddleware<T>
    where T : class
{
    private readonly Stack<IMiddleware<T>> middlewareStack = new();

    void IMiddlewareConfig<T>.Use(IMiddleware<T> middleware)
    {
        middlewareStack.Push(middleware);
    }

    public virtual ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        return middlewareStack.Pop().InvokeAsync(message, cancellationToken);
    }
}

public interface IMiddleware<T> where T : class
{
    ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default);
}

public interface IMiddlewareConfig<T>
    where T : class
{
    void Use(IMiddleware<T> middleware);
}

internal class CustomerMiddleware<T, TCustom>(TCustom middleware) : Middleware<T>
    where T : class
    where TCustom : IMiddleware<T>
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await middleware.InvokeAsync(message, cancellationToken);
        await base.InvokeAsync(message, cancellationToken);
    }
}

internal class RetryMiddleware<T> : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}

internal class CircuitBreakerMiddleware<T> : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}

internal class BatchMiddleware<T> : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}

internal class ThrottleMiddleware<T> : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}

internal class AwaitForgetMiddleware<T> : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        try
        {
            await base.InvokeAsync(message, cancellationToken);
        }
        catch (Exception)
        {
            //forget
        }
    }
}

internal class FireForgetMiddleware<T> : Middleware<T>
    where T : class
{
    public override ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = base.InvokeAsync(message, cancellationToken).AsTask();
        }
        catch (Exception)
        {
            //forget
        }
        return ValueTask.CompletedTask;
    }
}

internal class OutboxMiddleware<T> : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}

internal class InboxMiddleware<T> : Middleware<T>
    where T : class
{
    public override async ValueTask InvokeAsync(IEnvelope<T> message, CancellationToken cancellationToken = default)
    {
        await base.InvokeAsync(message, cancellationToken);
    }
}
