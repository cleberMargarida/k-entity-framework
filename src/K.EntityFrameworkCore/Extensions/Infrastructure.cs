using Microsoft.EntityFrameworkCore.Infrastructure;
#pragma warning disable IDE0079
#pragma warning disable EF1001

namespace K.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Class to hold a value of type <typeparamref name="T"/> without override the default user's DI container.
    /// </summary>
    internal sealed class Infrastructure<T>(T value) : IInfrastructure<T>
    {
        public T Instance => value;
    }

}
