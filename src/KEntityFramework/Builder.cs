using Confluent.Kafka;
using System;
using System.Linq.Expressions;

namespace KEntityFramework;

public class BrokerOptionsBuilder
{
    public BrokerOptionsBuilder WithBootstrapServers(string bootstrapServers)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslMechanism(SaslMechanism? mechanism)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithAcks(Acks? acks)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithClientId(string clientId)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithMessageMaxBytes(int? maxBytes)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSocketSendBufferBytes(int? bufferBytes)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSocketReceiveBufferBytes(int? bufferBytes)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableSocketKeepalive(bool? enable)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableSocketNagleDisable(bool? disable)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSocketMaxFails(int? maxFails)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithBrokerAddressTtl(int? ttlMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithBrokerAddressFamily(BrokerAddressFamily? family)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSocketConnectionSetupTimeoutMs(int? timeoutMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithConnectionsMaxIdleMs(int? maxIdleMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithReconnectBackoffMs(int? backoffMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithReconnectBackoffMaxMs(int? maxBackoffMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithStatisticsIntervalMs(int? intervalMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableLogQueue(bool? logQueue)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableLogThreadName(bool? logThreadName)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableRandomSeed(bool? enable)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableLogConnectionClose(bool? logConnectionClose)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithInternalTerminationSignal(int? signal)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableApiVersionRequest(bool? request)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithApiVersionRequestTimeoutMs(int? timeoutMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithApiVersionFallbackMs(int? fallbackMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithBrokerVersionFallback(string version)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableAllowAutoCreateTopics(bool? allow)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSecurityProtocol(SecurityProtocol? protocol)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslCipherSuites(string cipherSuites)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslCurvesList(string curvesList)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslSigalgsList(string sigalgsList)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslKeyLocation(string keyLocation)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslKeyPassword(string keyPassword)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslKeyPem(string keyPem)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslCertificateLocation(string certificateLocation)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslCertificatePem(string certificatePem)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslCaLocation(string caLocation)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslCaPem(string caPem)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslCaCertificateStores(string stores)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslCrlLocation(string crlLocation)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslKeystoreLocation(string keystoreLocation)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslKeystorePassword(string keystorePassword)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslProviders(string providers)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslEngineLocation(string engineLocation)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslEngineId(string engineId)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableSslCertificateVerification(bool? enable)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSslEndpointIdentificationAlgorithm(SslEndpointIdentificationAlgorithm? algorithm)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslKerberosServiceName(string serviceName)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslKerberosPrincipal(string principal)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslKerberosKinitCmd(string kinitCmd)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslKerberosKeytab(string keytab)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslKerberosMinTimeBeforeRelogin(int? minTimeMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslUsername(string username)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslPassword(string password)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslOauthbearerConfig(string config)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableSaslOauthbearerUnsecureJwt(bool? enable)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslOauthbearerMethod(SaslOauthbearerMethod? method)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslOauthbearerClientId(string clientId)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslOauthbearerClientSecret(string clientSecret)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslOauthbearerScope(string scope)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslOauthbearerExtensions(string extensions)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSaslOauthbearerTokenEndpointUrl(string tokenEndpointUrl)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithPluginLibraryPaths(string pluginLibraryPaths)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithClientRack(string clientRack)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithRetryBackoffMs(int? retryBackoffMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithRetryBackoffMaxMs(int? retryBackoffMaxMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithClientDnsLookup(ClientDnsLookup clientDnsLookup)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableMetricsPush(bool enableMetricsPush)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithMessageCopyMaxBytes(int? maxBytes)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithReceiveMessageMaxBytes(int? maxBytes)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithMaxInFlight(int? maxInFlight)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithTopicMetadataRefreshIntervalMs(int? intervalMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithMetadataMaxAgeMs(int? maxAgeMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithTopicMetadataRefreshFastIntervalMs(int? intervalMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder EnableTopicMetadataRefreshSparse(bool? refreshSparse)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithTopicMetadataPropagationMaxMs(int? maxMs)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithTopicBlacklist(string blacklist)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithDebug(string debug)
    {
        throw new NotImplementedException();
    }

    public BrokerOptionsBuilder WithSocketTimeoutMs(int? timeoutMs)
    {
        throw new NotImplementedException();
    }

    public TopicTypedBuilder<T> Event<T>()
    {
        throw new NotImplementedException();
    }
}
public class BrokerModelBuilder
{
    public BrokerModelBuilder Topic<TEvent>(Action<TopicTypedBuilder<TEvent>> topic)
    {
        throw new NotImplementedException();
    }
}

public class ConsumerGroupTypedBuilder<T>
{
    public ConsumerAdvancedFeatures Features { get; set; }

    public ConsumerGroupTypedBuilder<T> GroupId(string id)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> SetDeserializer(Action<DeserializerBuilder<T>> deserializer)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithConsumeResultFields(string fields)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithAutoOffsetReset(AutoOffsetReset offsetReset)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithGroupInstanceId(string groupInstanceId)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithPartitionAssignmentStrategy(PartitionAssignmentStrategy strategy)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithSessionTimeoutMs(int timeoutMs)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithHeartbeatIntervalMs(int intervalMs)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithGroupProtocolType(string protocolType)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithGroupProtocol(GroupProtocol protocol)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithGroupRemoteAssignor(string assignor)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithCoordinatorQueryIntervalMs(int intervalMs)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithMaxPollIntervalMs(int intervalMs)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> DisableAutoCommit()
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithAutoCommitIntervalMs(int intervalMs)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> EnableAutoOffsetStore()
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithQueuedMinMessages(int minMessages)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithQueuedMaxMessagesKbytes(int maxKbytes)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithFetchWaitMaxMs(int waitMs)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithFetchQueueBackoffMs(int backoffMs)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithMaxPartitionFetchBytes(int maxBytes)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithFetchMaxBytes(int maxBytes)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithFetchMinBytes(int minBytes)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithFetchErrorBackoffMs(int backoffMs)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> WithIsolationLevel(IsolationLevel isolationLevel)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> DisablePartitionEof()
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> EnableCrcCheck()
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> UseHandler<THandler>()
    {
        throw new NotImplementedException();
    }
}

public class TopicTypedBuilder<T>
{
    public TopicTypedBuilder<T> HasName(string topicName)
    {
        throw new NotImplementedException();
    }

    public TopicTypedBuilder<T> WithPartitions(int partitionCount)
    {
        throw new NotImplementedException();
    }

    public TopicTypedBuilder<T> WithReplicationFactor(short replicationFactor)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> HasProducer()
    {
        throw new NotImplementedException();
    }

    public TopicTypedBuilder<T> HasSetting(Func<TopicSettings, object> settings)
    {
        throw new NotImplementedException();
    }

    public ConsumerGroupTypedBuilder<T> HasConsumer()
    {
        throw new NotImplementedException();
    }
}

//public class ConsumerBuilder<T>
//{
//    public ConsumerBuilder<T> AddDeserializer(Action<DeserializerBuilder<T>> deserializer)
//    {
//        throw new NotImplementedException();
//    }
//}

public class DeserializerBuilder<T>
{
    public DeserializerBuilder<T> UseJsonDeserializer()
    {
        throw new NotImplementedException();
    }

    public DeserializerBuilder<T> Use<TSerializer>()
        where TSerializer : ISerializer<T>
    {
        throw new NotImplementedException();
    }
}

public class ProducerBuilder<T>
{
    public ProducerAdvancedFeatures<T> Features { get; }

    public ProducerBuilder<T> SetSerializer(Action<SerializerBuilder<T>> serializer)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> HasKey<TProp>(Expression<Func<T, TProp>> key)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> DisableBackgroundPoll()
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> DisableDeliveryReports()
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithDeliveryReportFields(string fields)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithRequestTimeout(int timeoutMs)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithMessageTimeout(int timeoutMs)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithPartitioner(Partitioner partitioner)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithCompressionLevel(int level)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithTransactionalId(string transactionalId)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithTransactionTimeout(int timeoutMs)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> EnableIdempotence()
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> EnableGaplessGuarantee()
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithQueueBufferingMaxMessages(int maxMessages)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithQueueBufferingMaxKbytes(int maxKbytes)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithLingerMs(double lingerMs)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithMessageSendMaxRetries(int maxRetries)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithQueueBufferingBackpressureThreshold(int threshold)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithCompressionType(CompressionType compressionType)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithBatchNumMessages(int numMessages)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithBatchSize(int size)
    {
        throw new NotImplementedException();
    }

    public ProducerBuilder<T> WithStickyPartitioningLingerMs(int lingerMs)
    {
        throw new NotImplementedException();
    }
}

public class ConsumerAdvancedFeatures
{
}

public class ProducerAdvancedFeatures<T>
{
    public ProducerAdvancedFeatures<T> AddBatching(Action<BatchingBuilder> batch)
    {
        throw new NotImplementedException();
    }

    public ProducerAdvancedFeatures<T> AddCircuitBreaker(Action<CircuitBreakerBuilder> circuitBreaker)
    {
        throw new NotImplementedException();
    }

    public ProducerAdvancedFeatures<T> AddOutbox(Action<OutboxBuilder> outbox)
    {
        throw new NotImplementedException();
    }

    public ProducerAdvancedFeatures<T> AddRetry(Action<RetryBuilder> retry)
    {
        throw new NotImplementedException();
    }
}

public class OutboxBuilder
{
    public OutboxBuilder UseInMemory()
    {
        throw new NotImplementedException();
    }

    public OutboxBuilder WithPollingPeriod(TimeSpan pollingPeriod)
    {
        throw new NotImplementedException();
    }
}

public class RetryBuilder
{
    public void WithIntervals(params double[] intervalsMilliseconds)
    {
        throw new NotImplementedException();
    }

    public void WithIntervals(params TimeSpan[] intervals)
    {
        throw new NotImplementedException();
    }

    public void WithImmediate(int retryLimit)
    {
        throw new NotImplementedException();
    }

    public void WithInfinitely()
    {
        throw new NotImplementedException();
    }

    public void WithIncremental(int retryLimit, TimeSpan initialInterval, TimeSpan intervalIncrement)
    {
        throw new NotImplementedException();
    }
}

public class CircuitBreakerBuilder
{
    public CircuitBreakerBuilder WithFailureThreshold(int threshold)
    {
        throw new NotImplementedException();
    }

    public CircuitBreakerBuilder WithOpenStateDuration(TimeSpan duration)
    {
        throw new NotImplementedException();
    }

    public CircuitBreakerBuilder WithHalfOpenRetryCount(int retryCount)
    {
        throw new NotImplementedException();
    }

    public CircuitBreakerBuilder WithRequestTimeout(TimeSpan timeout)
    {
        throw new NotImplementedException();
    }

    public CircuitBreakerBuilder WithHandledExceptions(params Type[] exceptions)
    {
        throw new NotImplementedException();
    }

    public CircuitBreakerBuilder DisableResetOnSuccess()
    {
        throw new NotImplementedException();
    }
}

public class SerializerBuilder<T>
{
    public SerializerBuilder<T> UseJsonSerializer()
    {
        throw new NotImplementedException();
    }

    public SerializerBuilder<T> Use<TSerializer>()
        where TSerializer : ISerializer<T>
    {
        throw new NotImplementedException();
    }
}

public class BatchingBuilder
{
    public BatchingBuilder WithMaxWaitTime(TimeSpan timeLimit)
    {
        throw new NotImplementedException();
    }

    public BatchingBuilder WithMaxCount(long count)
    {
        throw new NotImplementedException();
    }
}

internal class JsonTextSerializer<T> : ISerializer<T>
{
}

public interface ISerializer<T>
{
}