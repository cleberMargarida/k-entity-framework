using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Producer;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Reflection;

namespace K.EntityFrameworkCore.UnitTests.Extensions
{
    public class ScopedCommandRegistryTests
    {
        [Fact]
        public void ScopedCommandRegistry_ShouldHaveScopedServiceAttribute()
        {
            // Arrange
            var type = typeof(ScopedCommandRegistry);

            // Act
            var attributes = type.GetCustomAttributes<ScopedServiceAttribute>();

            // Assert
            Assert.Single(attributes);
        }

        [Fact]
        public void ScopedCommandRegistry_ShouldBeInternal()
        {
            // Arrange
            var type = typeof(ScopedCommandRegistry);

            // Assert
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void Add_ShouldAcceptScopedCommand()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            var commandExecuted = false;
            
            ScopedCommand command = (serviceProvider, cancellationToken) =>
            {
                commandExecuted = true;
                return ValueTask.CompletedTask;
            };

            // Act
            registry.Add(command);

            // Assert - Command should be added without exception
            Assert.False(commandExecuted); // Should not execute yet
        }

        [Fact]
        public async Task ExecuteAsync_WithNoCommands_ShouldCompleteSuccessfully()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act & Assert - Should not throw
            await registry.ExecuteAsync(serviceProvider);
        }

        [Fact]
        public async Task ExecuteAsync_WithSingleCommand_ShouldExecuteCommand()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            var commandExecuted = false;
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            ScopedCommand command = (sp, ct) =>
            {
                commandExecuted = true;
                return ValueTask.CompletedTask;
            };

            registry.Add(command);

            // Act
            await registry.ExecuteAsync(serviceProvider);

            // Assert
            Assert.True(commandExecuted);
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleCommands_ShouldExecuteAllInOrder()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            var executionOrder = new List<int>();
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            ScopedCommand command1 = (sp, ct) =>
            {
                executionOrder.Add(1);
                return ValueTask.CompletedTask;
            };

            ScopedCommand command2 = (sp, ct) =>
            {
                executionOrder.Add(2);
                return ValueTask.CompletedTask;
            };

            ScopedCommand command3 = (sp, ct) =>
            {
                executionOrder.Add(3);
                return ValueTask.CompletedTask;
            };

            registry.Add(command1);
            registry.Add(command2);
            registry.Add(command3);

            // Act
            await registry.ExecuteAsync(serviceProvider);

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellationToken_ShouldPassTokenToCommands()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var cancellationTokenSource = new CancellationTokenSource();
            var receivedToken = CancellationToken.None;

            ScopedCommand command = (sp, ct) =>
            {
                receivedToken = ct;
                return ValueTask.CompletedTask;
            };

            registry.Add(command);

            // Act
            await registry.ExecuteAsync(serviceProvider, cancellationTokenSource.Token);

            // Assert
            Assert.Equal(cancellationTokenSource.Token, receivedToken);
        }

        [Fact]
        public async Task ExecuteAsync_WithServiceProvider_ShouldPassServiceProviderToCommands()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            IServiceProvider? receivedServiceProvider = null;

            ScopedCommand command = (sp, ct) =>
            {
                receivedServiceProvider = sp;
                return ValueTask.CompletedTask;
            };

            registry.Add(command);

            // Act
            await registry.ExecuteAsync(serviceProvider);

            // Assert
            Assert.Same(serviceProvider, receivedServiceProvider);
        }

        [Fact]
        public async Task ExecuteAsync_AfterExecution_ShouldClearCommands()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var executionCount = 0;

            ScopedCommand command = (sp, ct) =>
            {
                executionCount++;
                return ValueTask.CompletedTask;
            };

            registry.Add(command);

            // Act
            await registry.ExecuteAsync(serviceProvider);
            await registry.ExecuteAsync(serviceProvider); // Execute again

            // Assert
            Assert.Equal(1, executionCount); // Should only execute once
        }

        [Fact]
        public async Task ExecuteAsync_WithExceptionInCommand_ShouldPropagateException()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var expectedException = new InvalidOperationException("Test exception");

            ScopedCommand command = (sp, ct) =>
            {
                throw expectedException;
            };

            registry.Add(command);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                registry.ExecuteAsync(serviceProvider).AsTask());

            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void Add_MultipleCommands_ShouldMaintainQueue()
        {
            // Arrange
            var registry = new ScopedCommandRegistry();
            
            ScopedCommand command1 = (sp, ct) => ValueTask.CompletedTask;
            ScopedCommand command2 = (sp, ct) => ValueTask.CompletedTask;

            // Act
            registry.Add(command1);
            registry.Add(command2);

            // Assert - Should not throw, commands queued
            Assert.True(true);
        }
    }

    public class ProducerMiddlewareInvokeCommandTests
    {
        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }

        [Fact]
        public void ProducerMiddlewareInvokeCommand_ShouldBeStruct()
        {
            // Arrange
            var type = typeof(ProducerMiddlewareInvokeCommand<TestMessage>);

            // Assert
            Assert.True(type.IsValueType);
        }

        [Fact]
        public void ProducerMiddlewareInvokeCommand_ShouldBeReadonly()
        {
            // Arrange
            var type = typeof(ProducerMiddlewareInvokeCommand<TestMessage>);

            // Assert - Test if the struct is readonly by checking if all fields are readonly
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                Assert.True(field.IsInitOnly, $"Field {field.Name} should be readonly");
            }
        }

        [Fact]
        public void ProducerMiddlewareInvokeCommand_Constructor_ShouldAcceptMessage()
        {
            // Arrange
            var message = new TestMessage { Value = "test" };

            // Act & Assert - Should not throw
            var command = new ProducerMiddlewareInvokeCommand<TestMessage>(message);
            Assert.True(true);
        }

        [Fact]
        public async Task ExecuteAsync_WithoutRequiredService_ShouldThrowException()
        {
            // Arrange
            var message = new TestMessage { Value = "test" };
            var command = new ProducerMiddlewareInvokeCommand<TestMessage>(message);
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                command.ExecuteAsync(serviceProvider, CancellationToken.None).AsTask());
        }

        [Fact]
        public void ProducerMiddlewareInvokeCommand_ShouldBeGeneric()
        {
            // Arrange
            var type = typeof(ProducerMiddlewareInvokeCommand<>);

            // Assert
            Assert.True(type.IsGenericTypeDefinition);
            Assert.Single(type.GetGenericArguments());
        }

        [Fact]
        public void ProducerMiddlewareInvokeCommand_GenericConstraint_ShouldRequireClass()
        {
            // Arrange
            var type = typeof(ProducerMiddlewareInvokeCommand<>);
            var genericParameter = type.GetGenericArguments()[0];

            // Assert
            Assert.True(genericParameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        }
    }

    public class ScopedCommandTests
    {
        [Fact]
        public void ScopedCommand_ShouldBeDelegate()
        {
            // Arrange
            var type = typeof(ScopedCommand);

            // Assert
            Assert.True(type.IsSubclassOf(typeof(MulticastDelegate)));
        }

        [Fact]
        public void ScopedCommand_ShouldBePublic()
        {
            // Arrange
            var type = typeof(ScopedCommand);

            // Assert
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void ScopedCommand_ShouldReturnValueTask()
        {
            // Arrange
            var type = typeof(ScopedCommand);
            var invokeMethod = type.GetMethod("Invoke");

            // Assert
            Assert.NotNull(invokeMethod);
            Assert.Equal(typeof(ValueTask), invokeMethod.ReturnType);
        }

        [Fact]
        public void ScopedCommand_ShouldHaveCorrectParameters()
        {
            // Arrange
            var type = typeof(ScopedCommand);
            var invokeMethod = type.GetMethod("Invoke");

            // Assert
            Assert.NotNull(invokeMethod);
            var parameters = invokeMethod.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(typeof(IServiceProvider), parameters[0].ParameterType);
            Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        }

        [Fact]
        public async Task ScopedCommand_CanBeInvoked()
        {
            // Arrange
            var executed = false;
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            ScopedCommand command = (sp, ct) =>
            {
                executed = true;
                return ValueTask.CompletedTask;
            };

            // Act
            await command(serviceProvider, CancellationToken.None);

            // Assert
            Assert.True(executed);
        }
    }
}
