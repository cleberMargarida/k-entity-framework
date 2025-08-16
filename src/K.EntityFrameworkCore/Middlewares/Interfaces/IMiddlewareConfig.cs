namespace K.EntityFrameworkCore.Middlewares.Interfaces;

public interface IMiddlewareConfig<T>
    where T : class
{
    void Use(IMiddleware<T> middleware);
}
