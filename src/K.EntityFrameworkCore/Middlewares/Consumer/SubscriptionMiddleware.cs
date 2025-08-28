using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Middleware responsible for managing subscription lifecycle.
/// This middleware ensures that subscriptions are activated before consuming and properly disposed afterward.
/// </summary>
[ScopedService]
internal class SubscriptionMiddleware<T>(IServiceProvider serviceProvider, SubscriptionMiddlewareSettings<T> settings) : Middleware<T>(settings), IDisposable
    where T : class
{
    private IDisposable? activationToken;

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        // Activate subscription if not already activated
        if (activationToken == null)
        {
            var subscriptionRegistry = serviceProvider.GetRequiredService<SubscriptionRegistry<T>>();
            activationToken = subscriptionRegistry.Activate();
        }

        // Continue with the pipeline
        await base.InvokeAsync(envelope, cancellationToken);
    }

    /// <summary>
    /// Disposes the subscription when the middleware is disposed.
    /// </summary>
    public void Dispose()
    {
        activationToken?.Dispose();
        activationToken = null;
    }
}

/// <summary>
/// Settings for the subscription middleware.
/// </summary>
internal class SubscriptionMiddlewareSettings<T> : MiddlewareSettings<T>
    where T : class
{
    public SubscriptionMiddlewareSettings()
    {
        // Subscription middleware should always be enabled as it's core functionality
        IsMiddlewareEnabled = true;
    }
}
