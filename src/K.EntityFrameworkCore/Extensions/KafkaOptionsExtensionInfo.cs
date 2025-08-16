using Microsoft.EntityFrameworkCore.Infrastructure;
#pragma warning disable IDE0079
#pragma warning disable EF1001

namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaOptionsExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => string.Empty;

        public override int GetServiceProviderHashCode()
        {
            return 0;
        }

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
        {
            return true;
        }
    }

}
