using Microsoft.EntityFrameworkCore.Infrastructure;

namespace K.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Class to hold a value of type <typeparamref name="T"/> without override the default user's DI container.
    /// </summary>
    internal sealed class Infrastructure<T>(T value) : IInfrastructure<T>, IDisposable
    {
        public T Instance => value;

        public void Dispose()
        {
            if (Instance is IDisposable disposable)
                disposable.Dispose();
        }
    }

}
