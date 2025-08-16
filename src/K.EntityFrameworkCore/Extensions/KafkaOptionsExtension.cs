using Confluent.Kafka;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable IDE0079
#pragma warning disable EF1001

namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaOptionsExtension : IDbContextOptionsExtension
    {
        private readonly ClientConfig client;

        public KafkaOptionsExtension(ClientConfig client)
        {
            this.client = client;
            Info = new KafkaOptionsExtensionInfo(this);

        }

        public DbContextOptionsExtensionInfo Info { get; }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddSingleton<IInfrastructure<ClientConfig>, Infrastructure<ClientConfig>>(_ => new(client));
        }

        public void Validate(IDbContextOptions options)
        {
        }
    }

}
