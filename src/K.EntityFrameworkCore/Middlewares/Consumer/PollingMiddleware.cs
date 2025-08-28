using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using Microsoft.Extensions.DependencyInjection;

namespace K.EntityFrameworkCore.Middlewares.Consumer;

/// <summary>
/// Middleware responsible for managing polling lifecycle.
/// This middleware ensures that the appropriate pollers are started before consuming messages.
/// </summary>
[ScopedService]
internal class PollingMiddleware<T>(IServiceProvider serviceProvider, PollingMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
    private bool pollingStarted;

    public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
    {
        // Start polling if not already started
        if (!pollingStarted)
        {
            var settings = serviceProvider.GetRequiredService<ConsumerMiddlewareSettings<T>>();
            var pollerManager = serviceProvider.GetRequiredService<IPollerManager>();

            if (settings.ExclusiveConnection)
            {
                pollerManager.EnsureDedicatedStarted(typeof(T));
            }
            else
            {
                pollerManager.EnsureSharedStarted();
            }

            pollingStarted = true;
        }

        // Continue with the pipeline
        await base.InvokeAsync(envelope, cancellationToken);
    }
}

/// <summary>
/// Settings for the polling middleware.
/// </summary>
[SingletonService]
internal class PollingMiddlewareSettings<T> : MiddlewareSettings<T>
    where T : class
{
    public PollingMiddlewareSettings()
    {
        // Polling middleware should always be enabled as it's core functionality
        IsMiddlewareEnabled = true;
    }
}
