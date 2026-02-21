using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Testcontainers.Kafka;
using Xunit;
using System.Threading.Tasks;

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

public class KafkaFixture : IAsyncLifetime
{
    public KafkaContainer Container => field ??= new KafkaBuilder()
        //.WithPortBinding(9092)
        .Build();

    public string BootstrapAddress => Container.GetBootstrapAddress();

    // xUnit v3's IAsyncLifetime now expects ValueTask
    public ValueTask InitializeAsync() => new ValueTask(Container.StartAsync());

    public ValueTask DisposeAsync() => Container.DisposeAsync();
}
