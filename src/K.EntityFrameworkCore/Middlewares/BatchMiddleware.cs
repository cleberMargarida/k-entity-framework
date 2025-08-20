using K.EntityFrameworkCore.MiddlewareOptions;
using Microsoft.Extensions.Logging;

namespace K.EntityFrameworkCore.Middlewares;

internal abstract class BatchMiddleware<T>(BatchMiddlewareOptions<T> options) : Middleware<T>(options)
    where T : class
{
}
