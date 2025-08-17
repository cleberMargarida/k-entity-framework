using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares;

//internal class CustomMiddleware<T, TCustom>(TCustom middleware) : IMiddleware<T>
//    where T : class
//    where TCustom : IMiddleware<T>
//{
//    public ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
//    {
//        return middleware.InvokeAsync(envelope, cancellationToken);
//    }
//}
