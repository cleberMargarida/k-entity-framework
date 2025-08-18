using HelloWorld;
using K.EntityFrameworkCore;
using K.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyDbContext>(optionsBuilder => optionsBuilder

    // Configure EF Core to use SQL Server
    .UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;Initial Catalog=Hello World")

    // Enable Kafka extensibility for EF Core (publishing/consuming integration)
    .UseKafkaExtensibility(client =>
    {
        client.BootstrapServers = "localhost:9092";
        //...
    }))

    // Add and configure the outbox worker (used when topics are outbox-enabled)
    .AddOutboxKafkaWorker<MyDbContext>(outbox =>
    {
        outbox.WithMaxMessagesPerPoll(100)
              .WithPollingInterval(1000)
              .UseSingleNode();
    });


using var app = builder.Build();

app.Start();

Console.ReadLine();

var scope = app.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

dbContext.Database.EnsureDeleted();
dbContext.Database.EnsureCreated();

// here you're intending to mark the entity to be persisted.
dbContext.Orders.Add(new Order { Status = "New" });

// here you're signing the event to be published.
dbContext.OrderEvents.Publish(new OrderCreated { OrderId = 1, Status = "Created" });

// here you're saving the changes to the database and publishing the event.
await dbContext.SaveChangesAsync();

Console.ReadLine();

// here you're starting to consume kafka and moving the iterator cursor to the next offset in the assigned partitions.
while (await dbContext.OrderEvents.MoveNextAsync())
{
    // here you're accessing event related to the current offset.
    _ = dbContext.OrderEvents.Current;

    // here you're commiting the offset of the current event.
    await dbContext.SaveChangesAsync();
}

namespace HelloWorld
{
    public class MyDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Order> Orders { get; set; }

        public Topic<OrderCreated> OrderEvents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Topic<OrderCreated>(topic =>
            {
                topic.HasName("order-created-topic");

                topic.UseSystemTextJson(settings =>
                {
                    settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

                topic.HasProducer(producer =>
                {
                    producer.HasKey(o => o.OrderId);
                    producer.HasOutbox(outbox =>
                    {
                        outbox.UseBackgroundOnly();
                    });
                });

                topic.HasConsumer(consumer =>
                {
                    consumer.GroupId("");
                });

                topic.HasSetting(_ => { });
            });

            base.OnModelCreating(modelBuilder);
        }
    }

    public class Order
    {
        public int Id { get; set; }
        public string Status { get; set; }
    }

    public class OrderCreated
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
    }
}
