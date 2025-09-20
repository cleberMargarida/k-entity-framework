using K.EntityFrameworkCore.Interfaces;

namespace K.EntityFrameworkCore.Middlewares.Core;

internal abstract class MiddlewareInvoker<T> : Middleware<T>
    where T : class
{
    private IMiddleware<T>? tail;

    protected void Use(IMiddleware<T> node)
    {
        if (node.IsDisabled)
        {
            return;
        }

        IMiddleware<T> @this = this;
        if (@this.Next is null)
        {
            @this.Next = node;
            this.tail = node;
        }
        else
        {
            this.tail!.Next = node;
            this.tail = node;
        }
    }
}