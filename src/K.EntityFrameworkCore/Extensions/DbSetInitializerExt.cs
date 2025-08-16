using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
#pragma warning disable IDE0079
#pragma warning disable EF1001

namespace K.EntityFrameworkCore.Extensions
{
    internal class DbSetInitializerExt(IDbSetFinder setFinder, IDbSetSource setSource) : DbSetInitializer(setFinder, setSource)
    {
        public override void InitializeSets(DbContext context)
        {
            // initialize Kafka Topics
            base.InitializeSets(context);
        }
    }

}
