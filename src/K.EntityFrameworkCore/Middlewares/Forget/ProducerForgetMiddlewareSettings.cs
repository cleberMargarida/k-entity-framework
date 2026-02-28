using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace K.EntityFrameworkCore.Middlewares.Forget;

/// <summary>
/// Producer-specific configuration options for the ForgetMiddleware.
/// Reads configuration from model annotations set during OnModelCreating.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ProducerForgetMiddlewareSettings<T>(IModel model) : ForgetMiddlewareSettings<T>(model.IsProducerForgetEnabled<T>())
    where T : class
{
    /// <summary>
    /// Gets the forget strategy from model annotations.
    /// Default is AwaitForget.
    /// </summary>
    public override ForgetStrategy Strategy => model.GetProducerForgetStrategy<T>() ?? ForgetStrategy.AwaitForget;

    /// <summary>
    /// Gets the forget timeout from model annotations.
    /// Default is 30 seconds.
    /// </summary>
    public override TimeSpan Timeout => model.GetProducerForgetTimeout<T>() ?? TimeSpan.FromSeconds(30);
}
