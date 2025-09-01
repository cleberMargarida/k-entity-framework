using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Testcontainers.Kafka;
using Xunit;

namespace K.EntityFrameworkCore.IntegrationTests.Infrastructure;

public class KafkaFixture : IAsyncLifetime
{
    public KafkaContainer Container => field ??= new KafkaBuilder()
        //.WithPortBinding(9092)
        .Build();

    public string BootstrapAddress => Container.GetBootstrapAddress();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}
