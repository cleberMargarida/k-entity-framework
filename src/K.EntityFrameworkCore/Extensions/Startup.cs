////using Confluent.Kafka;
//using K.EntityFrameworkCore.Middlewares.Consumer;
//using K.EntityFrameworkCore.Middlewares.Core;
//using K.EntityFrameworkCore.Middlewares.Forget;
//using K.EntityFrameworkCore.Middlewares.HeaderFilter;
//using K.EntityFrameworkCore.Middlewares.Inbox;
//using K.EntityFrameworkCore.Middlewares.Outbox;
//using K.EntityFrameworkCore.Middlewares.Producer;
//using K.EntityFrameworkCore.Middlewares.Serialization;
//using Microsoft.Extensions.DependencyInjection;
//using System.Reflection;

//namespace K.EntityFrameworkCore.Extensions;

//internal class Startup(KafkaClientBuilder client, Type contextType)
//{
//    public void ConfigureServices(IServiceCollection services)
//    {
     
//    }
//}

////services.AddSingleton((ProducerConfig)this.client.Producer);
////services.AddSingleton((ConsumerConfig)this.client.Consumer);
////services.AddSingleton((IConsumerProcessingConfig)this.client.Consumer);

////// https://github.com/confluentinc/confluent-kafka-dotnet/issues/197
////// One consumer per process
////services.AddSingleton(ConsumerFactory);

////// Register the central Kafka consumer poll service (shared) with lazy consumer factory
////services.AddSingleton(provider => new KafkaConsumerPollService(provider, () => provider.GetRequiredService<IConsumer>()));
////services.AddSingleton<PollerManager>();

////// https://github.com/confluentinc/confluent-kafka-dotnet/issues/1346
////// One producer per process
////services.AddSingleton(ProducerFactory);
