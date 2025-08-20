namespace K.EntityFrameworkCore.Middlewares;

internal abstract class BatchMiddleware<T>(BatchMiddlewareSettings<T> settings) : Middleware<T>(settings)
    where T : class
{
}
