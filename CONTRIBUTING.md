# Contributing to K-Entity-Framework

We love your input! We want to make contributing to K-Entity-Framework as easy and transparent as possible, whether it's:

- Reporting a bug
- Discussing the current state of the code
- Submitting a fix
- Proposing new features
- Becoming a maintainer

## Development Process

We use GitHub to host code, to track issues and feature requests, as well as accept pull requests.

### Pull Requests

Pull requests are the best way to propose changes to the codebase. We actively welcome your pull requests:

1. **Fork the repo** and create your branch from `master`
2. **Add tests** if you've added code that should be tested
3. **Update documentation** if you've changed APIs
4. **Ensure the test suite passes** by running `dotnet test`
5. **Make sure your code follows** the existing style
6. **Issue that pull request**!

## Development Setup

### Prerequisites

- .NET 6.0 SDK or later
- Docker (for integration tests)
- Your favorite IDE (Visual Studio, VS Code, JetBrains Rider)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/cleberMargarida/k-entity-framework.git
   cd k-entity-framework
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run unit tests**
   ```bash
   dotnet test tests/K.EntityFrameworkCore.UnitTests
   ```

5. **Run integration tests** (requires Docker)
   ```bash
   docker-compose up -d kafka
   dotnet test tests/K.EntityFrameworkCore.IntegrationTests
   ```

### Project Structure

```
src/
├── K.EntityFrameworkCore/           # Main library
├── K.EntityFrameworkCore.CodeGen/   # Source generators
tests/
├── K.EntityFrameworkCore.UnitTests/      # Unit tests
├── K.EntityFrameworkCore.IntegrationTests/ # Integration tests
samples/
├── HelloWorld/                      # Basic sample
docs/                               # Documentation
```

## Coding Guidelines

### Code Style

- **C# Conventions**: Follow standard C# naming conventions
- **Async/Await**: Use async/await consistently, avoid `.Result` or `.Wait()`
- **Null Handling**: Use nullable reference types and null-conditional operators
- **Error Handling**: Use exceptions for exceptional cases, return results for expected failures

### Example Code Style

```csharp
// Good ✅
public async Task<Result<OrderCreated>> CreateOrderAsync(
    CreateOrderRequest request, 
    CancellationToken cancellationToken = default)
{
    if (request?.CustomerId is null)
    {
        return Result<OrderCreated>.Failure("Customer ID is required");
    }

    try
    {
        var order = new OrderCreated
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow
        };

        await _brokerContext.OrderEvents.ProduceAsync(order, cancellationToken);
        
        return Result<OrderCreated>.Success(order);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
        return Result<OrderCreated>.Failure($"Failed to create order: {ex.Message}");
    }
}

// Bad ❌
public OrderCreated CreateOrder(CreateOrderRequest request)
{
    var order = new OrderCreated();
    order.OrderId = Guid.NewGuid().ToString();
    order.CustomerId = request.CustomerId; // No null check
    order.CreatedAt = DateTime.UtcNow;

    _brokerContext.OrderEvents.ProduceAsync(order).Wait(); // Blocking async
    
    return order;
}
```

### Documentation

- **XML Comments**: All public APIs must have XML documentation
- **README Updates**: Update relevant README files for new features
- **Examples**: Provide examples for new features
- **Migration Guides**: Include migration instructions for breaking changes

```csharp
/// <summary>
/// Configures message deduplication for the inbox middleware.
/// </summary>
/// <typeparam name="T">The message type</typeparam>
/// <param name="valueAccessor">Expression to extract deduplication values from the message</param>
/// <returns>The inbox builder for method chaining</returns>
/// <example>
/// <code>
/// inbox.DeduplicateBy(order => order.OrderId);
/// inbox.DeduplicateBy(payment => new { payment.OrderId, payment.Amount });
/// </code>
/// </example>
public InboxBuilder<T> DeduplicateBy<T>(Expression<Func<T, object>> valueAccessor) where T : class
```

## Testing Guidelines

### Unit Tests

- **Test Naming**: Use descriptive names that explain the scenario
- **AAA Pattern**: Arrange, Act, Assert structure
- **Mock Dependencies**: Use mocks for external dependencies
- **Test Edge Cases**: Include null values, empty collections, boundary conditions

```csharp
[Test]
public async Task DeduplicateBy_WithValidExpression_SetsDeduplicationAccessor()
{
    // Arrange
    var settings = new InboxMiddlewareSettings<TestMessage>();
    var builder = new InboxBuilder<TestMessage>(settings);

    // Act
    builder.DeduplicateBy(msg => msg.Id);

    // Assert
    Assert.That(settings.DeduplicationValueAccessor, Is.Not.Null);
}

[Test]
public async Task DeduplicateBy_WithNullExpression_ThrowsArgumentNullException()
{
    // Arrange
    var settings = new InboxMiddlewareSettings<TestMessage>();
    var builder = new InboxBuilder<TestMessage>(settings);

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => 
        builder.DeduplicateBy(null));
}
```

### Integration Tests

- **Real Kafka**: Use actual Kafka instances via test containers
- **Database**: Use in-memory or test databases
- **End-to-End**: Test complete scenarios from producer to consumer
- **Cleanup**: Ensure tests clean up resources

```csharp
[Test]
public async Task MessageRoundTrip_WithDeduplication_PreventsDuplicates()
{
    // Arrange
    using var kafkaContainer = new KafkaTestContainer();
    await kafkaContainer.StartAsync();
    
    var brokerContext = CreateBrokerContext(kafkaContainer.BootstrapServers);
    var testMessage = new TestMessage { Id = "test-123", Content = "Test" };

    // Act
    await brokerContext.TestTopic.ProduceAsync(testMessage);
    await brokerContext.TestTopic.ProduceAsync(testMessage); // Duplicate
    
    var results = new List<TestMessage>();
    for (int i = 0; i < 5; i++) // Try to consume multiple times
    {
        var result = await brokerContext.TestTopic.FirstAsync();
        if (result != null)
        {
            results.Add(result.Message);
            await brokerContext.TestTopic.CommitAsync();
        }
    }

    // Assert
    Assert.That(results.Count, Is.EqualTo(1), "Should only process message once");
    Assert.That(results[0].Id, Is.EqualTo("test-123"));
}
```

### Performance Tests

- **Benchmarks**: Use BenchmarkDotNet for performance tests
- **Memory Allocation**: Track memory usage and allocations
- **Throughput**: Measure messages per second
- **Latency**: Measure end-to-end latency

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class SerializationBenchmarks
{
    private readonly TestMessage _message = new() { Id = "test", Content = "content" };
    private readonly SystemTextJsonSerializer<TestMessage> _serializer = new(new JsonSerializerOptions());

    [Benchmark]
    public byte[] SerializeMessage()
    {
        return _serializer.Serialize(_message);
    }

    [Benchmark]
    public TestMessage DeserializeMessage()
    {
        var bytes = _serializer.Serialize(_message);
        return _serializer.Deserialize(bytes);
    }
}
```

## Feature Development Process

### 1. Discussion

- **GitHub Discussions**: Start with a discussion for new features
- **Issue Creation**: Create an issue describing the feature
- **Design Document**: For complex features, create a design document

### 2. Implementation

- **Feature Branch**: Create a branch from `master`
- **Incremental Commits**: Make small, focused commits
- **Tests First**: Write tests before implementation when possible
- **Documentation**: Update docs as you implement

### 3. Review Process

- **Self Review**: Review your own code before submitting
- **PR Description**: Provide clear description of changes
- **Breaking Changes**: Highlight any breaking changes
- **Migration Path**: Provide migration instructions if needed

### 4. Release Process

- **Version Bumping**: Follow semantic versioning
- **Change Log**: Update CHANGELOG.md
- **Release Notes**: Provide clear release notes
- **Documentation**: Ensure docs are up to date

## Issue Reporting

### Bug Reports

Great bug reports tend to have:

- **Quick Summary**: One-line summary of the issue
- **Steps to Reproduce**: Specific steps to reproduce the behavior
- **Expected Behavior**: What you expected to happen
- **Actual Behavior**: What actually happened
- **Environment**: OS, .NET version, library version
- **Code Sample**: Minimal code sample that reproduces the issue

### Feature Requests

Feature requests should include:

- **Problem Statement**: What problem does this solve?
- **Proposed Solution**: How should this work?
- **Alternatives**: What alternatives have you considered?
- **Use Cases**: Real-world scenarios where this would be useful

## Community Guidelines

### Code of Conduct

- **Be Respectful**: Treat everyone with respect and kindness
- **Be Constructive**: Provide constructive feedback and suggestions
- **Be Inclusive**: Welcome newcomers and help them learn
- **Be Professional**: Keep discussions professional and on-topic

### Communication

- **GitHub Issues**: For bug reports and feature requests
- **GitHub Discussions**: For questions and general discussion
- **Pull Request Reviews**: For code review and technical discussion

## Recognition

Contributors will be recognized in:

- **CONTRIBUTORS.md**: All contributors are listed
- **Release Notes**: Significant contributions are highlighted
- **GitHub**: Contributor badge and recognition

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

If you have questions about contributing, please:

1. Check the existing documentation
2. Search existing issues and discussions
3. Create a new discussion if needed

Thank you for contributing! 🎉
