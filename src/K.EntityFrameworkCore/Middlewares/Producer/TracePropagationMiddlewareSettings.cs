using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Producer;

/// <summary>
/// Settings for <see cref="TracePropagationMiddleware{T}"/>.
/// The middleware is enabled by default and disabled when tracing is opted out via <see cref="KafkaClientBuilder.DisableTracing"/>.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class TracePropagationMiddlewareSettings<T>(KafkaClientBuilder clientBuilder)
    : MiddlewareSettings<T>(isMiddlewareEnabled: !clientBuilder.IsTracingDisabled)
    where T : class
{
}
