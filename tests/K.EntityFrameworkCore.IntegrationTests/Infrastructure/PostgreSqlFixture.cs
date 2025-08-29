using Testcontainers.PostgreSql;
using Xunit;

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

public class PostgreSqlFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres => field ??= new PostgreSqlBuilder().Build();

    public string Connection => Postgres.GetConnectionString();

    public Task InitializeAsync() => Postgres.StartAsync();

    public Task DisposeAsync() => Postgres.DisposeAsync().AsTask();
}
