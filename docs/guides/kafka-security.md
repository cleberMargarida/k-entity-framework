# Kafka Security Configuration

This guide covers security configuration for Kafka clients in K-Entity-Framework, including TLS/SSL, SASL authentication mechanisms, and cloud-provider setups.

For general broker and topic configuration, see [Kafka Configuration](kafka-configuration.md).

## SSL/TLS Configuration

```csharp
kafka.SecurityProtocol = SecurityProtocol.Ssl;
kafka.SslCaLocation = "/path/to/ca-cert.pem";
kafka.SslCertificateLocation = "/path/to/client-cert.pem";
kafka.SslKeyLocation = "/path/to/client-key.pem";
kafka.SslKeyPassword = "key-password";
kafka.EnableSslCertificateVerification = true;
kafka.SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.Https;
```

## SASL Authentication

### SASL/PLAIN

```csharp
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.Plain;
kafka.SaslUsername = "my-username";
kafka.SaslPassword = "my-password";
```

### SASL/SCRAM

```csharp
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.ScramSha256;
kafka.SaslUsername = "my-username";
kafka.SaslPassword = "my-password";
```

### SASL/OAUTHBEARER

```csharp
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.OAuthBearer;
kafka.SaslOauthbearerConfig = "principalClaimName=sub";
```

### Kerberos (GSSAPI)

```csharp
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.Gssapi;
kafka.SaslKerberosServiceName = "kafka";
kafka.SaslKerberosPrincipal = "client@REALM.COM";
kafka.SaslKerberosKeytab = "/path/to/client.keytab";
```

## Cloud Provider Configurations

### Confluent Cloud

```csharp
kafka.BootstrapServers = "pkc-xxxxx.region.provider.confluent.cloud:9092";
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.Plain;
kafka.SaslUsername = "your-api-key";
kafka.SaslPassword = "your-api-secret";

// Optimize for cloud
kafka.Producer.CompressionType = CompressionType.Snappy;
kafka.Producer.BatchSize = 32768; // Larger batches for network efficiency
kafka.Producer.LingerMs = 10;
```

### Amazon MSK

```csharp
kafka.BootstrapServers = "b-1.msk-cluster.xxxxx.region.amazonaws.com:9092";

// MSK with IAM authentication
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.OAuthBearer;
kafka.SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc;
kafka.SaslOauthbearerClientId = "your-client-id";
kafka.SaslOauthbearerClientSecret = "your-client-secret";
kafka.SaslOauthbearerTokenEndpointUrl = "https://sts.region.amazonaws.com/";
```

### Azure Event Hubs

```csharp
kafka.BootstrapServers = "your-namespace.servicebus.windows.net:9093";
kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
kafka.SaslMechanism = SaslMechanism.Plain;
kafka.SaslUsername = "$ConnectionString";
kafka.SaslPassword = "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key";

// Event Hubs specific limits
kafka.Producer.MessageMaxBytes = 1000000; // 1 MB max message size
kafka.Consumer.FetchMaxBytes = 1048576;   // 1 MB fetch size
```

## Best Practices

- Always use `SaslSsl` in production — never `Plaintext` outside a trusted private network.
- Load credentials from environment variables or a secrets manager; never hard-code them.
- Rotate credentials regularly and use short-lived tokens (OAuthBearer/OIDC) where possible.

```csharp
if (builder.Environment.IsProduction())
{
    kafka.SecurityProtocol = SecurityProtocol.SaslSsl;
    kafka.SaslMechanism = SaslMechanism.ScramSha256;
    kafka.SaslUsername = Environment.GetEnvironmentVariable("KAFKA_USERNAME");
    kafka.SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD");
}
```

## Related

- [Kafka Configuration](kafka-configuration.md) — basic, producer, and consumer settings
- [Kafka Performance & Monitoring](kafka-performance.md) — tuning, health checks, debug logging
