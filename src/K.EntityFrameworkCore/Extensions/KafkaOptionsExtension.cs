using Confluent.Kafka;
using K.EntityFrameworkCore.MiddlewareOptions;
using K.EntityFrameworkCore.Middlewares;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable IDE0079
#pragma warning disable EF1001

namespace K.EntityFrameworkCore.Extensions
{
    internal class KafkaOptionsExtension : IDbContextOptionsExtension
    {
        internal static IDbContextOptions? CachedOptions;

        private readonly ClientConfig client;

        public KafkaOptionsExtension(ClientConfig client)
        {
            this.client = client;
            Info = new KafkaOptionsExtensionInfo(this);
        }

        public DbContextOptionsExtensionInfo Info { get; }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddScoped(typeof(ConsumerMiddlewareInvoker<>));
            services.AddScoped(typeof(ProducerMiddlewareInvoker<>));

            services.AddSingleton(typeof(AwaitForgetMiddlewareOptions<>));
            services.AddScoped(typeof(AwaitForgetMiddleware<>));

            services.AddSingleton(typeof(BatchMiddlewareOptions<>));
            services.AddScoped(typeof(BatchMiddleware<>));

            services.AddSingleton(typeof(FireForgetMiddlewareOptions<>));
            services.AddScoped(typeof(FireForgetMiddleware<>));

            services.AddSingleton(typeof(CircuitBreakerMiddlewareOptions<>));
            services.AddScoped(typeof(CircuitBreakerMiddleware<>));

            services.AddSingleton(typeof(RetryMiddlewareOptions<>));
            services.AddScoped(typeof(RetryMiddleware<>));

            services.AddSingleton(typeof(InboxMiddlewareOptions<>));
            services.AddScoped(typeof(InboxMiddleware<>));

            services.AddSingleton(typeof(OutboxMiddlewareOptions<>));
            services.AddScoped(typeof(OutboxMiddleware<>));

            services.AddSingleton(typeof(ThrottleMiddlewareOptions<>));
            services.AddScoped(typeof(ThrottleMiddleware<>));

            services.AddSingleton<Infrastructure<ClientConfig>>(_ => new(client));
        }

        public void Validate(IDbContextOptions options)
        {
            this.CachedOptions = options;
        }
    }
}
