using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares
{
    internal abstract class MiddlewareInvoker<T> : Middleware<T>
        where T : class
    {
        private IMiddleware<T>? tail;

        protected void Use(IMiddleware<T> node)
        {
            if (!node.IsEnabled)
            {
                return;
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
        }
    }
}