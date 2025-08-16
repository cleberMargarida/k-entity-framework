using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

internal class ConsumerMiddlewareInvoker<T> : Middleware<T>
    where T : class
{
    public ConsumerMiddlewareInvoker()
    {
        //Use();
    }
}
