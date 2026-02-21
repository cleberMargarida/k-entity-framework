using Testcontainers.PostgreSql;

using System.Threading.Tasks;

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

public class PostgreSqlFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres => field ??= new PostgreSqlBuilder().Build();

    public string Connection => Postgres.GetConnectionString();

    // IAsyncLifetime now returns ValueTask
    public ValueTask InitializeAsync() => new ValueTask(Postgres.StartAsync());

    public ValueTask DisposeAsync() => Postgres.DisposeAsync();
}
