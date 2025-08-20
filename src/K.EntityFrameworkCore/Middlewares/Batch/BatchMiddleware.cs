using K.EntityFrameworkCore.Middlewares.Core;

namespace K.EntityFrameworkCore.Middlewares.Batch;

internal abstract class BatchMiddleware<T>(BatchMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
}
