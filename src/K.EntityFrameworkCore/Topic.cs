

namespace K.EntityFrameworkCore;

public class Topic<T> : IAsyncEnumerator<T>, IAsyncDisposable
    where T : class
{
    public T? Current => throw new NotImplementedException();

    public void Publish(in T domainEvent)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> MoveNextAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}
