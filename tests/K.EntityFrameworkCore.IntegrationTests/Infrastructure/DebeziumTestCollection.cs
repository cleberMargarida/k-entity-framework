namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

/// <summary>
/// Collection that wires <see cref="DebeziumFixture"/> (docker-compose stack) to all
/// Debezium integration tests.  Parallelisation is disabled because the tests share
/// fixed localhost ports exposed by docker-compose.
/// </summary>
[CollectionDefinition("DebeziumTests", DisableParallelization = true)]
public class DebeziumTestCollection : ICollectionFixture<DebeziumFixture>;
