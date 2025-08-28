using Xunit;

namespace K.EntityFrameworkCore.IntegrationTests
{
    /// <summary>
    /// Collection definition for integration tests to ensure proper isolation and resource management
    /// </summary>
    [CollectionDefinition("IntegrationTests", DisableParallelization = true)]
    public class IntegrationTestCollection
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
