using K.EntityFrameworkCore.Interfaces;
using System.Collections.ObjectModel;

namespace K.EntityFrameworkCore.Middlewares
{
    internal abstract class MiddlewareInvoker<T> : Middleware<T> 
        where T : class
    {
        private readonly Collection<IMiddleware<T>> middlewareStack = [];

        protected void Use(IMiddleware<T> middleware)
        {
            if (middleware.IsEnabled)
                middlewareStack.Add(middleware);
        }

        public override async ValueTask InvokeAsync(Envelope<T> envelope, CancellationToken cancellationToken = default)
        {
            foreach (var middleware in middlewareStack)
            {
                await middleware.InvokeAsync(envelope, cancellationToken);
            }
        }
    }
}