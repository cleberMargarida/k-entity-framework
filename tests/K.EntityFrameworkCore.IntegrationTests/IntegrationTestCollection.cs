using Xunit;
using K.EntityFrameworkCore.IntegrationTests.Infrastructure;

namespace K.EntityFrameworkCore.IntegrationTests
{
    /// <summary>
    /// Collection definition for integration tests to ensure proper isolation and resource management.
    /// Uses a shared fixture to initialize TestContainers once for all test classes in the collection.
    /// </summary>
    [CollectionDefinition("IntegrationTests", DisableParallelization = true)]
    public class IntegrationTestCollection 
        : ICollectionFixture<PostgreSqlFixture>
        , ICollectionFixture<KafkaFixture>
        , ICollectionFixture<WebApplicationFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
