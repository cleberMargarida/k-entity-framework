using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using System.Threading.Channels;

namespace K.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for configuring Kafka in a DbContext.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures the DbContext to use Kafka extensibility features with the specified bootstrap servers.
    /// This enables message production and consumption capabilities through middleware pipelines.
    /// </summary>
    /// <param name="optionsBuilder">The options builder for the DbContext.</param>
    /// <param name="bootstrapServers">Comma-separated list of Kafka bootstrap servers (e.g., "localhost:9092").</param>
    /// <returns>The same options builder instance for method chaining.</returns>
    public static DbContextOptionsBuilder UseKafkaExtensibility(this DbContextOptionsBuilder optionsBuilder, string bootstrapServers)
    {
        return UseKafkaExtensibility(optionsBuilder, client => client.BootstrapServers = bootstrapServers);
    }

    /// <summary>
    /// Configures the DbContext to use Kafka extensibility features with the specified client configuration.
    /// This enables message production and consumption capabilities through middleware pipelines.
    /// </summary>
    /// <param name="optionsBuilder">The options builder for the DbContext.</param>
    /// <param name="client">Action to configure the Kafka client settings.</param>
    /// <returns>The same options builder instance for method chaining.</returns>
    public static DbContextOptionsBuilder UseKafkaExtensibility(this DbContextOptionsBuilder optionsBuilder, Action<KafkaClientBuilder> client)
    {
        var clientInstance = new KafkaClientBuilder();
        client.Invoke(clientInstance);
        IDbContextOptionsBuilderInfrastructure infrastructure = optionsBuilder;
        infrastructure.AddOrUpdateExtension(new KafkaOptionsExtension(clientInstance, optionsBuilder.Options.ContextType));
        optionsBuilder.AddInterceptors(new KafkaMiddlewareInterceptor());
        optionsBuilder.ReplaceService<IDbSetInitializer, DbSetInitializerExt>();
        return optionsBuilder;
    }
}

/// <summary>
/// Builder for Kafka client configuration that provides a fluent API for setting up Kafka clients.
/// This class wraps the underlying Confluent.Kafka ClientConfig and provides additional configuration options.
/// </summary>
/// <param name="clientConfig">The underlying Confluent.Kafka ClientConfig instance.</param>
public class KafkaClientBuilder(ClientConfig clientConfig) : IClientConfig
{
    private readonly Lazy<ProducerConfigInternal> producerConfigInternal = new(() => new ProducerConfigInternal(clientConfig.ToDictionary()));
    private readonly Lazy<ConsumerConfigInternal> consumerConfigInternal = new(() => new ConsumerConfigInternal(clientConfig));

    internal KafkaClientBuilder() : this(new ClientConfig())
    {
    }

    /// <summary>
    /// Gets a value indicating whether distributed tracing is disabled.
    /// </summary>
    internal bool IsTracingDisabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether metrics collection is disabled.
    /// </summary>
    internal bool IsMetricsDisabled { get; private set; }

    /// <summary>
    /// Disables OpenTelemetry distributed tracing for this Kafka client.
    /// When disabled, trace context will not be propagated through message headers.
    /// </summary>
    /// <returns>The same builder instance for method chaining.</returns>
    public KafkaClientBuilder DisableTracing()
    {
        IsTracingDisabled = true;
        return this;
    }

    /// <summary>
    /// Disables OpenTelemetry metrics collection for this Kafka client.
    /// When disabled, counters and histograms will not be recorded.
    /// </summary>
    /// <returns>The same builder instance for method chaining.</returns>
    public KafkaClientBuilder DisableMetrics()
    {
        IsMetricsDisabled = true;
        return this;
    }

    /// <summary>
    /// Gets the underlying Confluent.Kafka ClientConfig instance.
    /// </summary>
    internal ClientConfig ClientConfig => clientConfig;

    /// <summary>
    /// Producer configuration.
    /// </summary>
    public IProducerConfig Producer => this.producerConfigInternal.Value;

    /// <summary>
    /// Consumer configuration.
    /// </summary>
    public IConsumerConfig Consumer => this.consumerConfigInternal.Value;

    /// <inheritdoc />
    public Acks? Acks
    {
        get => clientConfig.Acks;
        set => clientConfig.Acks = value;
    }

    /// <inheritdoc />
    public bool? AllowAutoCreateTopics
    {
        get => clientConfig.AllowAutoCreateTopics;
        set => clientConfig.AllowAutoCreateTopics = value;
    }

    /// <inheritdoc />
    public int? ApiVersionFallbackMs
    {
        get => clientConfig.ApiVersionFallbackMs;
        set => clientConfig.ApiVersionFallbackMs = value;
    }

    /// <inheritdoc />
    public bool? ApiVersionRequest
    {
        get => clientConfig.ApiVersionRequest;
        set => clientConfig.ApiVersionRequest = value;
    }

    /// <inheritdoc />
    public int? ApiVersionRequestTimeoutMs
    {
        get => clientConfig.ApiVersionRequestTimeoutMs;
        set => clientConfig.ApiVersionRequestTimeoutMs = value;
    }

    /// <inheritdoc />
    public string? BootstrapServers
    {
        get => clientConfig.BootstrapServers;
        set => clientConfig.BootstrapServers = value;
    }

    /// <inheritdoc />
    public BrokerAddressFamily? BrokerAddressFamily
    {
        get => clientConfig.BrokerAddressFamily;
        set => clientConfig.BrokerAddressFamily = value;
    }

    /// <inheritdoc />
    public int? BrokerAddressTtl
    {
        get => clientConfig.BrokerAddressTtl;
        set => clientConfig.BrokerAddressTtl = value;
    }

    /// <inheritdoc />
    public string? BrokerVersionFallback
    {
        get => clientConfig.BrokerVersionFallback;
        set => clientConfig.BrokerVersionFallback = value;
    }

    /// <inheritdoc />
    public ClientDnsLookup? ClientDnsLookup
    {
        get => clientConfig.ClientDnsLookup;
        set => clientConfig.ClientDnsLookup = value;
    }

    /// <inheritdoc />
    public string? ClientId
    {
        get => clientConfig.ClientId;
        set => clientConfig.ClientId = value;
    }

    /// <inheritdoc />
    public string? ClientRack
    {
        get => clientConfig.ClientRack;
        set => clientConfig.ClientRack = value;
    }

    /// <inheritdoc />
    public int? ConnectionsMaxIdleMs
    {
        get => clientConfig.ConnectionsMaxIdleMs;
        set => clientConfig.ConnectionsMaxIdleMs = value;
    }

    /// <inheritdoc />
    public string? Debug
    {
        get => clientConfig.Debug;
        set => clientConfig.Debug = value;
    }

    /// <inheritdoc />
    public bool? EnableMetricsPush
    {
        get => clientConfig.EnableMetricsPush;
        set => clientConfig.EnableMetricsPush = value;
    }

    /// <inheritdoc />
    public bool? EnableRandomSeed
    {
        get => clientConfig.EnableRandomSeed;
        set => clientConfig.EnableRandomSeed = value;
    }

    /// <inheritdoc />
    public bool? EnableSaslOauthbearerUnsecureJwt
    {
        get => clientConfig.EnableSaslOauthbearerUnsecureJwt;
        set => clientConfig.EnableSaslOauthbearerUnsecureJwt = value;
    }

    /// <inheritdoc />
    public bool? EnableSslCertificateVerification
    {
        get => clientConfig.EnableSslCertificateVerification;
        set => clientConfig.EnableSslCertificateVerification = value;
    }

    /// <inheritdoc />
    public int? InternalTerminationSignal
    {
        get => clientConfig.InternalTerminationSignal;
        set => clientConfig.InternalTerminationSignal = value;
    }

    /// <inheritdoc />
    public bool? LogConnectionClose
    {
        get => clientConfig.LogConnectionClose;
        set => clientConfig.LogConnectionClose = value;
    }

    /// <inheritdoc />
    public bool? LogQueue
    {
        get => clientConfig.LogQueue;
        set => clientConfig.LogQueue = value;
    }

    /// <inheritdoc />
    public bool? LogThreadName
    {
        get => clientConfig.LogThreadName;
        set => clientConfig.LogThreadName = value;
    }

    /// <inheritdoc />
    public int? MaxInFlight
    {
        get => clientConfig.MaxInFlight;
        set => clientConfig.MaxInFlight = value;
    }

    /// <inheritdoc />
    public int? MessageCopyMaxBytes
    {
        get => clientConfig.MessageCopyMaxBytes;
        set => clientConfig.MessageCopyMaxBytes = value;
    }

    /// <inheritdoc />
    public int? MessageMaxBytes
    {
        get => clientConfig.MessageMaxBytes;
        set => clientConfig.MessageMaxBytes = value;
    }

    /// <inheritdoc />
    public int? MetadataMaxAgeMs
    {
        get => clientConfig.MetadataMaxAgeMs;
        set => clientConfig.MetadataMaxAgeMs = value;
    }

    /// <inheritdoc />
    public MetadataRecoveryStrategy? MetadataRecoveryStrategy
    {
        get => clientConfig.MetadataRecoveryStrategy;
        set => clientConfig.MetadataRecoveryStrategy = value;
    }

    /// <inheritdoc />
    public string? PluginLibraryPaths
    {
        get => clientConfig.PluginLibraryPaths;
        set => clientConfig.PluginLibraryPaths = value;
    }

    /// <inheritdoc />
    public int? ReceiveMessageMaxBytes
    {
        get => clientConfig.ReceiveMessageMaxBytes;
        set => clientConfig.ReceiveMessageMaxBytes = value;
    }

    /// <inheritdoc />
    public int? ReconnectBackoffMaxMs
    {
        get => clientConfig.ReconnectBackoffMaxMs;
        set => clientConfig.ReconnectBackoffMaxMs = value;
    }

    /// <inheritdoc />
    public int? ReconnectBackoffMs
    {
        get => clientConfig.ReconnectBackoffMs;
        set => clientConfig.ReconnectBackoffMs = value;
    }

    /// <inheritdoc />
    public int? RetryBackoffMaxMs
    {
        get => clientConfig.RetryBackoffMaxMs;
        set => clientConfig.RetryBackoffMaxMs = value;
    }

    /// <inheritdoc />
    public int? RetryBackoffMs
    {
        get => clientConfig.RetryBackoffMs;
        set => clientConfig.RetryBackoffMs = value;
    }

    /// <inheritdoc />
    public string? SaslKerberosKeytab
    {
        get => clientConfig.SaslKerberosKeytab;
        set => clientConfig.SaslKerberosKeytab = value;
    }

    /// <inheritdoc />
    public string? SaslKerberosKinitCmd
    {
        get => clientConfig.SaslKerberosKinitCmd;
        set => clientConfig.SaslKerberosKinitCmd = value;
    }

    /// <inheritdoc />
    public int? SaslKerberosMinTimeBeforeRelogin
    {
        get => clientConfig.SaslKerberosMinTimeBeforeRelogin;
        set => clientConfig.SaslKerberosMinTimeBeforeRelogin = value;
    }

    /// <inheritdoc />
    public string? SaslKerberosPrincipal
    {
        get => clientConfig.SaslKerberosPrincipal;
        set => clientConfig.SaslKerberosPrincipal = value;
    }

    /// <inheritdoc />
    public string? SaslKerberosServiceName
    {
        get => clientConfig.SaslKerberosServiceName;
        set => clientConfig.SaslKerberosServiceName = value;
    }

    /// <inheritdoc />
    public SaslMechanism? SaslMechanism
    {
        get => clientConfig.SaslMechanism;
        set => clientConfig.SaslMechanism = value;
    }

    /// <inheritdoc />
    public string? SaslOauthbearerClientId
    {
        get => clientConfig.SaslOauthbearerClientId;
        set => clientConfig.SaslOauthbearerClientId = value;
    }

    /// <inheritdoc />
    public string? SaslOauthbearerClientSecret
    {
        get => clientConfig.SaslOauthbearerClientSecret;
        set => clientConfig.SaslOauthbearerClientSecret = value;
    }

    /// <inheritdoc />
    public string? SaslOauthbearerConfig
    {
        get => clientConfig.SaslOauthbearerConfig;
        set => clientConfig.SaslOauthbearerConfig = value;
    }

    /// <inheritdoc />
    public string? SaslOauthbearerExtensions
    {
        get => clientConfig.SaslOauthbearerExtensions;
        set => clientConfig.SaslOauthbearerExtensions = value;
    }

    /// <inheritdoc />
    public SaslOauthbearerMethod? SaslOauthbearerMethod
    {
        get => clientConfig.SaslOauthbearerMethod;
        set => clientConfig.SaslOauthbearerMethod = value;
    }

    /// <inheritdoc />
    public string? SaslOauthbearerScope
    {
        get => clientConfig.SaslOauthbearerScope;
        set => clientConfig.SaslOauthbearerScope = value;
    }

    /// <inheritdoc />
    public string? SaslOauthbearerTokenEndpointUrl
    {
        get => clientConfig.SaslOauthbearerTokenEndpointUrl;
        set => clientConfig.SaslOauthbearerTokenEndpointUrl = value;
    }

    /// <inheritdoc />
    public string? SaslPassword
    {
        get => clientConfig.SaslPassword;
        set => clientConfig.SaslPassword = value;
    }

    /// <inheritdoc />
    public string? SaslUsername
    {
        get => clientConfig.SaslUsername;
        set => clientConfig.SaslUsername = value;
    }

    /// <inheritdoc />
    public SecurityProtocol? SecurityProtocol
    {
        get => clientConfig.SecurityProtocol;
        set => clientConfig.SecurityProtocol = value;
    }

    /// <inheritdoc />
    public int? SocketConnectionSetupTimeoutMs
    {
        get => clientConfig.SocketConnectionSetupTimeoutMs;
        set => clientConfig.SocketConnectionSetupTimeoutMs = value;
    }

    /// <inheritdoc />
    public bool? SocketKeepaliveEnable
    {
        get => clientConfig.SocketKeepaliveEnable;
        set => clientConfig.SocketKeepaliveEnable = value;
    }

    /// <inheritdoc />
    public int? SocketMaxFails
    {
        get => clientConfig.SocketMaxFails;
        set => clientConfig.SocketMaxFails = value;
    }

    /// <inheritdoc />
    public bool? SocketNagleDisable
    {
        get => clientConfig.SocketNagleDisable;
        set => clientConfig.SocketNagleDisable = value;
    }

    /// <inheritdoc />
    public int? SocketReceiveBufferBytes
    {
        get => clientConfig.SocketReceiveBufferBytes;
        set => clientConfig.SocketReceiveBufferBytes = value;
    }

    /// <inheritdoc />
    public int? SocketSendBufferBytes
    {
        get => clientConfig.SocketSendBufferBytes;
        set => clientConfig.SocketSendBufferBytes = value;
    }

    /// <inheritdoc />
    public int? SocketTimeoutMs
    {
        get => clientConfig.SocketTimeoutMs;
        set => clientConfig.SocketTimeoutMs = value;
    }

    /// <inheritdoc />
    public string? SslCaCertificateStores
    {
        get => clientConfig.SslCaCertificateStores;
        set => clientConfig.SslCaCertificateStores = value;
    }

    /// <inheritdoc />
    public string? SslCaLocation
    {
        get => clientConfig.SslCaLocation;
        set => clientConfig.SslCaLocation = value;
    }

    /// <inheritdoc />
    public string? SslCaPem
    {
        get => clientConfig.SslCaPem;
        set => clientConfig.SslCaPem = value;
    }

    /// <inheritdoc />
    public string? SslCertificateLocation
    {
        get => clientConfig.SslCertificateLocation;
        set => clientConfig.SslCertificateLocation = value;
    }

    /// <inheritdoc />
    public string? SslCertificatePem
    {
        get => clientConfig.SslCertificatePem;
        set => clientConfig.SslCertificatePem = value;
    }

    /// <inheritdoc />
    public string? SslCipherSuites
    {
        get => clientConfig.SslCipherSuites;
        set => clientConfig.SslCipherSuites = value;
    }

    /// <inheritdoc />
    public string? SslCrlLocation
    {
        get => clientConfig.SslCrlLocation;
        set => clientConfig.SslCrlLocation = value;
    }

    /// <inheritdoc />
    public string? SslCurvesList
    {
        get => clientConfig.SslCurvesList;
        set => clientConfig.SslCurvesList = value;
    }

    /// <inheritdoc />
    public SslEndpointIdentificationAlgorithm? SslEndpointIdentificationAlgorithm
    {
        get => clientConfig.SslEndpointIdentificationAlgorithm;
        set => clientConfig.SslEndpointIdentificationAlgorithm = value;
    }

    /// <inheritdoc />
    public string? SslEngineId
    {
        get => clientConfig.SslEngineId;
        set => clientConfig.SslEngineId = value;
    }

    /// <inheritdoc />
    public string? SslEngineLocation
    {
        get => clientConfig.SslEngineLocation;
        set => clientConfig.SslEngineLocation = value;
    }

    /// <inheritdoc />
    public string? SslKeyLocation
    {
        get => clientConfig.SslKeyLocation;
        set => clientConfig.SslKeyLocation = value;
    }

    /// <inheritdoc />
    public string? SslKeyPassword
    {
        get => clientConfig.SslKeyPassword;
        set => clientConfig.SslKeyPassword = value;
    }

    /// <inheritdoc />
    public string? SslKeyPem
    {
        get => clientConfig.SslKeyPem;
        set => clientConfig.SslKeyPem = value;
    }

    /// <inheritdoc />
    public string? SslKeystoreLocation
    {
        get => clientConfig.SslKeystoreLocation;
        set => clientConfig.SslKeystoreLocation = value;
    }

    /// <inheritdoc />
    public string? SslKeystorePassword
    {
        get => clientConfig.SslKeystorePassword;
        set => clientConfig.SslKeystorePassword = value;
    }

    /// <inheritdoc />
    public string? SslProviders
    {
        get => clientConfig.SslProviders;
        set => clientConfig.SslProviders = value;
    }

    /// <inheritdoc />
    public string? SslSigalgsList
    {
        get => clientConfig.SslSigalgsList;
        set => clientConfig.SslSigalgsList = value;
    }

    /// <inheritdoc />
    public int? StatisticsIntervalMs
    {
        get => clientConfig.StatisticsIntervalMs;
        set => clientConfig.StatisticsIntervalMs = value;
    }

    /// <inheritdoc />
    public string? TopicBlacklist
    {
        get => clientConfig.TopicBlacklist;
        set => clientConfig.TopicBlacklist = value;
    }

    /// <inheritdoc />
    public int? TopicMetadataPropagationMaxMs
    {
        get => clientConfig.TopicMetadataPropagationMaxMs;
        set => clientConfig.TopicMetadataPropagationMaxMs = value;
    }

    /// <inheritdoc />
    public int? TopicMetadataRefreshFastIntervalMs
    {
        get => clientConfig.TopicMetadataRefreshFastIntervalMs;
        set => clientConfig.TopicMetadataRefreshFastIntervalMs = value;
    }

    /// <inheritdoc />
    public int? TopicMetadataRefreshIntervalMs
    {
        get => clientConfig.TopicMetadataRefreshIntervalMs;
        set => clientConfig.TopicMetadataRefreshIntervalMs = value;
    }

    /// <inheritdoc />
    public bool? TopicMetadataRefreshSparse
    {
        get => clientConfig.TopicMetadataRefreshSparse;
        set => clientConfig.TopicMetadataRefreshSparse = value;
    }
}

/// <summary>
/// Interface for Kafka client configuration options.
/// </summary>
public interface IClientConfig
{
    /// <summary>
    /// The acks parameter controls how many partition replicas must receive the record before the producer can consider the write successful.
    /// </summary>
    Acks? Acks { get; set; }

    /// <summary>
    /// Allow automatic topic creation on the broker when subscribing to or assigning non-existent topics.
    /// </summary>
    bool? AllowAutoCreateTopics { get; set; }

    /// <summary>
    /// Dictates how long the broker.version.fallback lookup time is. Only used if ApiVersionRequest is false.
    /// </summary>
    int? ApiVersionFallbackMs { get; set; }

    /// <summary>
    /// Request broker's supported API versions to adjust functionality to available protocol features.
    /// </summary>
    bool? ApiVersionRequest { get; set; }

    /// <summary>
    /// Timeout for broker API version requests.
    /// </summary>
    int? ApiVersionRequestTimeoutMs { get; set; }

    /// <summary>
    /// Initial list of Kafka brokers as a CSV list of broker host or host:port.
    /// </summary>
    string? BootstrapServers { get; set; }

    /// <summary>
    /// Address family preference when performing DNS lookups.
    /// </summary>
    BrokerAddressFamily? BrokerAddressFamily { get; set; }

    /// <summary>
    /// How long to cache the broker address resolving results (milliseconds).
    /// </summary>
    int? BrokerAddressTtl { get; set; }

    /// <summary>
    /// Older broker versions (before 0.10.0) provide no way for a client to query for supported protocol features.
    /// </summary>
    string? BrokerVersionFallback { get; set; }

    /// <summary>
    /// Controls how the client uses DNS lookups.
    /// </summary>
    ClientDnsLookup? ClientDnsLookup { get; set; }

    /// <summary>
    /// Client identifier.
    /// </summary>
    string? ClientId { get; set; }

    /// <summary>
    /// A rack identifier for this client.
    /// </summary>
    string? ClientRack { get; set; }

    /// <summary>
    /// Close idle connections after the number of milliseconds specified by this config.
    /// </summary>
    int? ConnectionsMaxIdleMs { get; set; }

    /// <summary>
    /// A comma-separated list of debug contexts to enable.
    /// </summary>
    string? Debug { get; set; }

    /// <summary>
    /// Whether to enable pushing metrics to an external service.
    /// </summary>
    bool? EnableMetricsPush { get; set; }

    /// <summary>
    /// If enabled librdkafka will initialize the PRNG with srand(current_time.nanoseconds) on the first invocation of rd_kafka_new().
    /// </summary>
    bool? EnableRandomSeed { get; set; }

    /// <summary>
    /// Enable the builtin unsecure JWT OAUTHBEARER token handler if no oauthbearer_refresh_cb has been set.
    /// </summary>
    bool? EnableSaslOauthbearerUnsecureJwt { get; set; }

    /// <summary>
    /// Enable OpenSSL's builtin broker (server) certificate verification.
    /// </summary>
    bool? EnableSslCertificateVerification { get; set; }

    /// <summary>
    /// Signal that librdkafka will use to quickly terminate on rd_kafka_destroy().
    /// </summary>
    int? InternalTerminationSignal { get; set; }

    /// <summary>
    /// Log broker disconnects.
    /// </summary>
    bool? LogConnectionClose { get; set; }

    /// <summary>
    /// Enable queue statistics.
    /// </summary>
    bool? LogQueue { get; set; }

    /// <summary>
    /// Include thread name in log messages.
    /// </summary>
    bool? LogThreadName { get; set; }

    /// <summary>
    /// Maximum number of in-flight requests per broker connection.
    /// </summary>
    int? MaxInFlight { get; set; }

    /// <summary>
    /// Maximum size for message copying. Messages larger than this will be referenced rather than copied.
    /// </summary>
    int? MessageCopyMaxBytes { get; set; }

    /// <summary>
    /// Maximum Kafka protocol request message size.
    /// </summary>
    int? MessageMaxBytes { get; set; }

    /// <summary>
    /// The period of time in milliseconds after which we force a refresh of metadata.
    /// </summary>
    int? MetadataMaxAgeMs { get; set; }

    /// <summary>
    /// How to recover from broker stalls or unavailable brokers.
    /// </summary>
    MetadataRecoveryStrategy? MetadataRecoveryStrategy { get; set; }

    /// <summary>
    /// List of plugin libraries to load separated by ;.
    /// </summary>
    string? PluginLibraryPaths { get; set; }

    /// <summary>
    /// Maximum receive message size.
    /// </summary>
    int? ReceiveMessageMaxBytes { get; set; }

    /// <summary>
    /// The maximum amount of time in milliseconds to wait when reconnecting to a broker that has repeatedly failed to connect.
    /// </summary>
    int? ReconnectBackoffMaxMs { get; set; }

    /// <summary>
    /// The base amount of time to wait before attempting to reconnect to a given host.
    /// </summary>
    int? ReconnectBackoffMs { get; set; }

    /// <summary>
    /// The maximum amount of time in milliseconds to wait before retrying.
    /// </summary>
    int? RetryBackoffMaxMs { get; set; }

    /// <summary>
    /// The amount of time to wait before attempting to retry a failed request.
    /// </summary>
    int? RetryBackoffMs { get; set; }

    /// <summary>
    /// Path to Kerberos keytab file.
    /// </summary>
    string? SaslKerberosKeytab { get; set; }

    /// <summary>
    /// Full kerberos kinit command string.
    /// </summary>
    string? SaslKerberosKinitCmd { get; set; }

    /// <summary>
    /// Minimum time in milliseconds between key refresh attempts.
    /// </summary>
    int? SaslKerberosMinTimeBeforeRelogin { get; set; }

    /// <summary>
    /// This client's Kerberos principal name.
    /// </summary>
    string? SaslKerberosPrincipal { get; set; }

    /// <summary>
    /// The Kerberos principal name that Kafka runs as.
    /// </summary>
    string? SaslKerberosServiceName { get; set; }

    /// <summary>
    /// SASL mechanism to use for authentication.
    /// </summary>
    SaslMechanism? SaslMechanism { get; set; }

    /// <summary>
    /// OAuth/OIDC client identifier.
    /// </summary>
    string? SaslOauthbearerClientId { get; set; }

    /// <summary>
    /// OAuth/OIDC client secret.
    /// </summary>
    string? SaslOauthbearerClientSecret { get; set; }

    /// <summary>
    /// SASL/OAUTHBEARER configuration.
    /// </summary>
    string? SaslOauthbearerConfig { get; set; }

    /// <summary>
    /// SASL/OAUTHBEARER extensions.
    /// </summary>
    string? SaslOauthbearerExtensions { get; set; }

    /// <summary>
    /// Set to "default" or "oidc" to control which login method to be used.
    /// </summary>
    SaslOauthbearerMethod? SaslOauthbearerMethod { get; set; }

    /// <summary>
    /// OAuth/OIDC scope for the token.
    /// </summary>
    string? SaslOauthbearerScope { get; set; }

    /// <summary>
    /// OAuth/OIDC token endpoint URL.
    /// </summary>
    string? SaslOauthbearerTokenEndpointUrl { get; set; }

    /// <summary>
    /// SASL password for use with the PLAIN and SASL-SCRAM-.. mechanisms.
    /// </summary>
    string? SaslPassword { get; set; }

    /// <summary>
    /// SASL username for use with the PLAIN and SASL-SCRAM-.. mechanisms.
    /// </summary>
    string? SaslUsername { get; set; }

    /// <summary>
    /// Protocol used to communicate with brokers.
    /// </summary>
    SecurityProtocol? SecurityProtocol { get; set; }

    /// <summary>
    /// Maximum time allowed for broker connection setup.
    /// </summary>
    int? SocketConnectionSetupTimeoutMs { get; set; }

    /// <summary>
    /// Enable TCP keep-alives (SO_KEEPALIVE) on broker sockets.
    /// </summary>
    bool? SocketKeepaliveEnable { get; set; }

    /// <summary>
    /// Disconnect from broker when this number of send failures has been reached.
    /// </summary>
    int? SocketMaxFails { get; set; }

    /// <summary>
    /// Disable the Nagle algorithm (TCP_NODELAY) on broker sockets.
    /// </summary>
    bool? SocketNagleDisable { get; set; }

    /// <summary>
    /// Broker socket receive buffer size.
    /// </summary>
    int? SocketReceiveBufferBytes { get; set; }

    /// <summary>
    /// Broker socket send buffer size.
    /// </summary>
    int? SocketSendBufferBytes { get; set; }

    /// <summary>
    /// Default timeout for network requests.
    /// </summary>
    int? SocketTimeoutMs { get; set; }

    /// <summary>
    /// Certificate store list to read CA certificate from.
    /// </summary>
    string? SslCaCertificateStores { get; set; }

    /// <summary>
    /// File or directory path to CA certificate(s) for verifying the broker's key.
    /// </summary>
    string? SslCaLocation { get; set; }

    /// <summary>
    /// CA certificate as a string in PEM format.
    /// </summary>
    string? SslCaPem { get; set; }

    /// <summary>
    /// Path to client's public key (PEM) used for authentication.
    /// </summary>
    string? SslCertificateLocation { get; set; }

    /// <summary>
    /// Client's public key as a string in PEM format.
    /// </summary>
    string? SslCertificatePem { get; set; }

    /// <summary>
    /// A cipher suite is a named combination of authentication, encryption, MAC and key exchange algorithm.
    /// </summary>
    string? SslCipherSuites { get; set; }

    /// <summary>
    /// Path to CRL for verifying broker's certificate validity.
    /// </summary>
    string? SslCrlLocation { get; set; }

    /// <summary>
    /// The supported-curves extension in the TLS ClientHello message specifies the curves.
    /// </summary>
    string? SslCurvesList { get; set; }

    /// <summary>
    /// The endpoint identification algorithm used by clients to validate server hostname.
    /// </summary>
    SslEndpointIdentificationAlgorithm? SslEndpointIdentificationAlgorithm { get; set; }

    /// <summary>
    /// The identifier (string) of the cryptographic engine to use.
    /// </summary>
    string? SslEngineId { get; set; }

    /// <summary>
    /// Path to the cryptographic engine library.
    /// </summary>
    string? SslEngineLocation { get; set; }

    /// <summary>
    /// Path to client's private key (PEM) used for authentication.
    /// </summary>
    string? SslKeyLocation { get; set; }

    /// <summary>
    /// Private key passphrase (for use with ssl.key.location and set_ssl_cert()).
    /// </summary>
    string? SslKeyPassword { get; set; }

    /// <summary>
    /// Client's private key as a string in PEM format.
    /// </summary>
    string? SslKeyPem { get; set; }

    /// <summary>
    /// Path to client's keystore (PKCS#12) used for authentication.
    /// </summary>
    string? SslKeystoreLocation { get; set; }

    /// <summary>
    /// Client's keystore (PKCS#12) password.
    /// </summary>
    string? SslKeystorePassword { get; set; }

    /// <summary>
    /// List of OpenSSL providers to load and use.
    /// </summary>
    string? SslProviders { get; set; }

    /// <summary>
    /// The client uses the TLS ClientHello signature_algorithms extension to indicate to the server which signature/hash algorithm pairs.
    /// </summary>
    string? SslSigalgsList { get; set; }

    /// <summary>
    /// librdkafka statistics emit interval.
    /// </summary>
    int? StatisticsIntervalMs { get; set; }

    /// <summary>
    /// Topic blacklist, a comma-separated list of regular expressions for matching topic names.
    /// </summary>
    string? TopicBlacklist { get; set; }

    /// <summary>
    /// Apache Kafka topic metadata propagation maximum time in milliseconds.
    /// </summary>
    int? TopicMetadataPropagationMaxMs { get; set; }

    /// <summary>
    /// Topic metadata refresh interval in milliseconds.
    /// </summary>
    int? TopicMetadataRefreshFastIntervalMs { get; set; }

    /// <summary>
    /// Period of time in milliseconds after which we force a refresh of metadata.
    /// </summary>
    int? TopicMetadataRefreshIntervalMs { get; set; }

    /// <summary>
    /// Sparse metadata requests enable Kafka to request only the locally used topics.
    /// </summary>
    bool? TopicMetadataRefreshSparse { get; set; }
}

/// <summary>
/// Interface for shared operational configuration between producer and consumer.
/// Contains properties that can be configured differently for producers vs consumers.
/// </summary>
public interface IWorkerConfig
{
    /// <summary>
    /// The amount of time to wait before attempting to retry a failed request.
    /// </summary>
    int? RetryBackoffMs { get; set; }

    /// <summary>
    /// The maximum amount of time in milliseconds to wait before retrying.
    /// </summary>
    int? RetryBackoffMaxMs { get; set; }

    /// <summary>
    /// The base amount of time to wait before attempting to reconnect to a given host.
    /// </summary>
    int? ReconnectBackoffMs { get; set; }

    /// <summary>
    /// The maximum amount of time in milliseconds to wait when reconnecting to a broker that has repeatedly failed to connect.
    /// </summary>
    int? ReconnectBackoffMaxMs { get; set; }

    /// <summary>
    /// Maximum number of in-flight requests per broker connection.
    /// </summary>
    int? MaxInFlight { get; set; }

    /// <summary>
    /// Default timeout for network requests.
    /// </summary>
    int? SocketTimeoutMs { get; set; }

    /// <summary>
    /// Broker socket send buffer size.
    /// </summary>
    int? SocketSendBufferBytes { get; set; }

    /// <summary>
    /// Broker socket receive buffer size.
    /// </summary>
    int? SocketReceiveBufferBytes { get; set; }

    /// <summary>
    /// Enable TCP keep-alives (SO_KEEPALIVE) on broker sockets.
    /// </summary>
    bool? SocketKeepaliveEnable { get; set; }

    /// <summary>
    /// Disable the Nagle algorithm (TCP_NODELAY) on broker sockets.
    /// </summary>
    bool? SocketNagleDisable { get; set; }

    /// <summary>
    /// Disconnect from broker when this number of send failures has been reached.
    /// </summary>
    int? SocketMaxFails { get; set; }

    /// <summary>
    /// Maximum time allowed for broker connection setup.
    /// </summary>
    int? SocketConnectionSetupTimeoutMs { get; set; }

    /// <summary>
    /// Close idle connections after the number of milliseconds specified by this config.
    /// </summary>
    int? ConnectionsMaxIdleMs { get; set; }

    /// <summary>
    /// Maximum Kafka protocol request message size.
    /// </summary>
    int? MessageMaxBytes { get; set; }

    /// <summary>
    /// Maximum receive message size.
    /// </summary>
    int? ReceiveMessageMaxBytes { get; set; }

    /// <summary>
    /// Maximum size for message copying. Messages larger than this will be referenced rather than copied.
    /// </summary>
    int? MessageCopyMaxBytes { get; set; }

    /// <summary>
    /// librdkafka statistics emit interval.
    /// </summary>
    int? StatisticsIntervalMs { get; set; }

    /// <summary>
    /// A comma-separated list of debug contexts to enable.
    /// </summary>
    string? Debug { get; set; }

    /// <summary>
    /// Log broker disconnects.
    /// </summary>
    bool? LogConnectionClose { get; set; }

    /// <summary>
    /// Enable queue statistics.
    /// </summary>
    bool? LogQueue { get; set; }

    /// <summary>
    /// Include thread name in log messages.
    /// </summary>
    bool? LogThreadName { get; set; }
}

/// <summary>
/// Interface for Kafka consumer message processing options.
/// Controls how messages are buffered and processed within the framework.
/// </summary>
public interface IConsumerProcessingConfig
{
    /// <summary>
    /// The maximum number of messages that can be buffered in memory per message type before applying backpressure.
    /// Default is 1000. Higher values provide better throughput but use more memory.
    /// Lower values reduce memory usage but may limit processing throughput.
    /// </summary>
    int MaxBufferedMessages { get; }

    /// <summary>
    /// The behavior when the message buffer is full.
    /// Default is ApplyBackpressure, which slows down message consumption to prevent memory exhaustion.
    /// </summary>
    ConsumerBackpressureMode BackpressureMode { get; }

    /// <summary>
    /// The ratio of channel capacity at which the consumer should be paused.
    /// Default is 0.80 (80%). Must be between 0.0 and 1.0.
    /// </summary>
    double HighWaterMarkRatio { get; }

    /// <summary>
    /// The ratio of channel capacity at which a paused consumer should be resumed.
    /// Default is 0.50 (50%). Must be between 0.0 and 1.0 and less than <see cref="HighWaterMarkRatio"/>.
    /// </summary>
    double LowWaterMarkRatio { get; }

    /// <summary>
    /// Creates internal channel options from this consumer configuration.
    /// </summary>
    internal BoundedChannelOptions ToBoundedChannelOptions()
    {
        return new BoundedChannelOptions(MaxBufferedMessages)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BackpressureMode switch
            {
                ConsumerBackpressureMode.DropOldestMessage => BoundedChannelFullMode.DropOldest,
                ConsumerBackpressureMode.DropNewestMessage => BoundedChannelFullMode.DropWrite,
                ConsumerBackpressureMode.ApplyBackpressure => BoundedChannelFullMode.Wait,
                _ => BoundedChannelFullMode.Wait,
            },
            AllowSynchronousContinuations = false
        };
    }
}

/// <summary>
/// Defines how the consumer should behave when the message buffer reaches capacity.
/// </summary>
public enum ConsumerBackpressureMode
{
    /// <summary>
    /// Applies backpressure by slowing down message consumption.
    /// This is the recommended mode for most scenarios as it prevents message loss.
    /// </summary>
    ApplyBackpressure,

    /// <summary>
    /// Drops the oldest buffered message when a new message arrives.
    /// Use with caution as this can result in message loss.
    /// </summary>
    DropOldestMessage,

    /// <summary>
    /// Drops the newest message when the buffer is full.
    /// Use with caution as this can result in message loss.
    /// </summary>
    DropNewestMessage
}


/// <summary>
/// Interface for Kafka consumer configuration options.
/// </summary>
public interface IConsumerConfig : IWorkerConfig, IConsumerProcessingConfig
{
    /// <summary>
    /// The ratio of channel capacity at which the consumer should be paused.
    /// Default is 0.80 (80%). Must be between 0.0 and 1.0.
    /// </summary>
    new double HighWaterMarkRatio { get; set; }

    /// <summary>
    /// The ratio of channel capacity at which a paused consumer should be resumed.
    /// Default is 0.50 (50%). Must be between 0.0 and 1.0 and less than <see cref="HighWaterMarkRatio"/>.
    /// </summary>
    new double LowWaterMarkRatio { get; set; }

    /// <summary>
    /// The frequency in milliseconds that the consumer offsets are auto-committed to Kafka if enable.auto.commit is set to true.
    /// </summary>
    int? AutoCommitIntervalMs { get; set; }

    /// <summary>
    /// Action to take when there is no initial offset in Kafka or if the current offset does not exist any more on the server.
    /// </summary>
    AutoOffsetReset? AutoOffsetReset { get; set; }

    /// <summary>
    /// Automatically check the CRC32 of the records consumed.
    /// </summary>
    bool? CheckCrcs { get; set; }

    /// <summary>
    /// How often to query for the current client group coordinator.
    /// </summary>
    int? CoordinatorQueryIntervalMs { get; set; }

    /// <summary>
    /// If true the consumer's offset will be periodically committed in the background.
    /// </summary>
    bool? EnableAutoCommit { get; set; }

    /// <summary>
    /// Automatically store offset of last message provided to application.
    /// </summary>
    bool? EnableAutoOffsetStore { get; set; }

    /// <summary>
    /// Emit RD_KAFKA_RESP_ERR__PARTITION_EOF event whenever the consumer reaches the end of a partition.
    /// </summary>
    bool? EnablePartitionEof { get; set; }

    /// <summary>
    /// How long to postpone the next fetch request for a topic+partition in case of a fetch error.
    /// </summary>
    int? FetchErrorBackoffMs { get; set; }

    /// <summary>
    /// Maximum amount of data the server should return for a fetch request.
    /// </summary>
    int? FetchMaxBytes { get; set; }

    /// <summary>
    /// Minimum amount of data the server should return for a fetch request.
    /// </summary>
    int? FetchMinBytes { get; set; }

    /// <summary>
    /// How long to postpone the next fetch request for a topic+partition in case the current fetch queue is non-empty.
    /// </summary>
    int? FetchQueueBackoffMs { get; set; }

    /// <summary>
    /// Maximum amount of time the server will block before answering the fetch request if there isn't sufficient data to immediately satisfy the requirement given by fetch.min.bytes.
    /// </summary>
    int? FetchWaitMaxMs { get; set; }

    /// <summary>
    /// Client group id string. All clients sharing the same group.id belong to the same group.
    /// </summary>
    string? GroupId { get; set; }

    /// <summary>
    /// Enable static group membership.
    /// </summary>
    string? GroupInstanceId { get; set; }

    /// <summary>
    /// Group protocol to use.
    /// </summary>
    GroupProtocol? GroupProtocol { get; set; }

    /// <summary>
    /// Consumer group protocol type.
    /// </summary>
    string? GroupProtocolType { get; set; }

    /// <summary>
    /// The name of one or more partition assignment strategies.
    /// </summary>
    string? GroupRemoteAssignor { get; set; }

    /// <summary>
    /// The expected time between heartbeats to the consumer coordinator when using Kafka's group management facility.
    /// </summary>
    int? HeartbeatIntervalMs { get; set; }

    /// <summary>
    /// Controls how to read messages which have been written to Kafka with a transactional producer.
    /// </summary>
    IsolationLevel? IsolationLevel { get; set; }

    /// <summary>
    /// Maximum amount of data per-partition the server will return.
    /// </summary>
    int? MaxPartitionFetchBytes { get; set; }

    /// <summary>
    /// The maximum delay between invocations of poll() when using consumer group management.
    /// </summary>
    int? MaxPollIntervalMs { get; set; }

    /// <summary>
    /// The name of one or more partition assignment strategies.
    /// </summary>
    PartitionAssignmentStrategy? PartitionAssignmentStrategy { get; set; }

    /// <summary>
    /// Maximum number of kilobytes of queued pre-fetched messages in the local consumer queue.
    /// </summary>
    int? QueuedMaxMessagesKbytes { get; set; }

    /// <summary>
    /// Minimum number of messages per topic+partition librdkafka tries to maintain in the local consumer queue.
    /// </summary>
    int? QueuedMinMessages { get; set; }

    /// <summary>
    /// Client group session and failure detection timeout.
    /// </summary>
    int? SessionTimeoutMs { get; set; }
}

/// <summary>
/// Interface for Kafka producer configuration options.
/// </summary>
public interface IProducerConfig : IWorkerConfig
{
    /// <summary>
    /// Maximum number of messages batched in one MessageSet.
    /// </summary>
    int? BatchNumMessages { get; set; }

    /// <summary>
    /// Maximum size (in bytes) of all messages batched in one MessageSet.
    /// </summary>
    int? BatchSize { get; set; }

    /// <summary>
    /// Compression level parameter for algorithms that support it.
    /// </summary>
    int? CompressionLevel { get; set; }

    /// <summary>
    /// Compression codec to use for compressing message sets.
    /// </summary>
    CompressionType? CompressionType { get; set; }

    /// <summary>
    /// Only provide delivery reports for failed messages.
    /// </summary>
    string? DeliveryReportFields { get; set; }

    /// <summary>
    /// Librdkafka background thread polling.
    /// </summary>
    bool? EnableBackgroundPoll { get; set; }

    /// <summary>
    /// Request delivery reports.
    /// </summary>
    bool? EnableDeliveryReports { get; set; }

    /// <summary>
    /// Enable gapless delivery guarantee.
    /// </summary>
    bool? EnableGaplessGuarantee { get; set; }

    /// <summary>
    /// When set to true, the producer will ensure that messages are successfully produced exactly once and in the original produce order.
    /// </summary>
    bool? EnableIdempotence { get; set; }

    /// <summary>
    /// Delay in milliseconds to wait for messages in the producer queue to accumulate before constructing message batches.
    /// </summary>
    double? LingerMs { get; set; }

    /// <summary>
    /// How many times to retry sending a failing Message.
    /// </summary>
    int? MessageSendMaxRetries { get; set; }

    /// <summary>
    /// Local message timeout.
    /// </summary>
    int? MessageTimeoutMs { get; set; }

    /// <summary>
    /// Partitioner to use when partitioning messages.
    /// </summary>
    Partitioner? Partitioner { get; set; }

    /// <summary>
    /// The backpressure threshold for the producer's message queue.
    /// </summary>
    int? QueueBufferingBackpressureThreshold { get; set; }

    /// <summary>
    /// Maximum total message size sum allowed on the producer queue.
    /// </summary>
    int? QueueBufferingMaxKbytes { get; set; }

    /// <summary>
    /// Maximum number of messages allowed on the producer queue.
    /// </summary>
    int? QueueBufferingMaxMessages { get; set; }

    /// <summary>
    /// The ack timeout of the producer request in milliseconds.
    /// </summary>
    int? RequestTimeoutMs { get; set; }

    /// <summary>
    /// Delay in milliseconds to wait to assign new sticky partitions for each topic.
    /// </summary>
    int? StickyPartitioningLingerMs { get; set; }

    /// <summary>
    /// The TransactionalId to use for transactional delivery.
    /// </summary>
    string? TransactionalId { get; set; }

    /// <summary>
    /// The maximum amount of time in milliseconds that the transaction coordinator will wait for a transaction status update.
    /// </summary>
    int? TransactionTimeoutMs { get; set; }
}

internal class ProducerConfigInternal(Dictionary<string, string> clientConfig) : ProducerConfig(clientConfig), IProducerConfig
{
    // ISharedOperationalConfig implementation - properties that can be configured separately for producers
    // These delegate to the underlying ProducerConfig which inherits from ClientConfig

    // All producer-specific properties are automatically implemented through inheritance from ProducerConfig
    // All shared operational properties are automatically implemented through inheritance from ClientConfig
}

internal class ConsumerConfigInternal : ConsumerConfig, IConsumerConfig
{
    public ConsumerConfigInternal(ClientConfig clientConfig) : base(clientConfig.ToDictionary())
    {
        // set default settings
        GroupId = AppDomain.CurrentDomain.FriendlyName;
        AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
        EnableAutoCommit = true;
        AutoCommitIntervalMs = 1000;
        EnableAutoOffsetStore = false;
    }

    private double _highWaterMarkRatio = 0.80;
    private double _lowWaterMarkRatio = 0.50;

    public int MaxBufferedMessages { get; set; } = 10_000;
    public ConsumerBackpressureMode BackpressureMode { get; set; } = ConsumerBackpressureMode.ApplyBackpressure;

    public double HighWaterMarkRatio
    {
        get => _highWaterMarkRatio;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);
            if (value <= _lowWaterMarkRatio)
                throw new ArgumentOutOfRangeException(nameof(HighWaterMarkRatio), value,
                    $"HighWaterMarkRatio ({value}) must be greater than LowWaterMarkRatio ({_lowWaterMarkRatio}).");
            _highWaterMarkRatio = value;
        }
    }

    public double LowWaterMarkRatio
    {
        get => _lowWaterMarkRatio;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, 1.0);
            if (value >= _highWaterMarkRatio)
                throw new ArgumentOutOfRangeException(nameof(LowWaterMarkRatio), value,
                    $"LowWaterMarkRatio ({value}) must be less than HighWaterMarkRatio ({_highWaterMarkRatio}).");
            _lowWaterMarkRatio = value;
        }
    }

    // ISharedOperationalConfig implementation - properties that can be configured separately for consumers
    // These delegate to the underlying ConsumerConfig which inherits from ClientConfig

    // All consumer-specific properties are automatically implemented through inheritance from ConsumerConfig
    // All shared operational properties are automatically implemented through inheritance from ClientConfig
}
