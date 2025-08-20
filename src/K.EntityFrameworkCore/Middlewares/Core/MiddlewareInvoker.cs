using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares.Core
{
    internal abstract class MiddlewareInvoker<T> : Middleware<T>
        where T : class
    {
        private IMiddleware<T>? tail;

        protected bool Use(IMiddleware<T> node)
        {
            if (!node.IsEnabled)
            {
                return false;
            }

            IMiddleware<T> @this = this;
            if (@this.Next is null)
            {
                @this.Next = node;
                tail = node;
            }
            else
            {
                tail!.Next = node;
                tail = node;
            }

            return true;
        }
    }
}