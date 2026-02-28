using K.EntityFrameworkCore.Interfaces;
using K.EntityFrameworkCore.Middlewares.Core;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Middlewares;

/// <summary>
/// Tests for <see cref="CustomMiddleware{T}"/> internal adapter.
/// </summary>
public class CustomMiddlewareTests
{
    private sealed record TestMessage(int Id, string Name);

    #region Test doubles

    /// <summary>
    /// A test middleware that records whether InvokeAsync was called and tracks its IsEnabled state.
    /// </summary>
    private sealed class SpyMiddleware : IMiddleware<TestMessage>
    {
        public bool InvokeWasCalled { get; private set; }
        public bool IsEnabled { get; init; } = true;

        IMiddleware<TestMessage>? IMiddleware<TestMessage>.Next { get; set; }

        public ValueTask<TestMessage?> InvokeAsync(Envelope<TestMessage> envelope, CancellationToken cancellationToken = default)
        {
            InvokeWasCalled = true;
            return ValueTask.FromResult<TestMessage?>(envelope.Message);
        }
    }

    /// <summary>
    /// A test invoker that exposes Use() for testing chain behavior.
    /// </summary>
    private sealed class TestInvoker : MiddlewareInvoker<TestMessage>
    {
        public void AddMiddleware(IMiddleware<TestMessage> middleware) => Use(middleware);
    }

    /// <summary>
    /// A terminal middleware that captures whether it was reached in the chain.
    /// </summary>
    private sealed class TerminalMiddleware : Middleware<TestMessage>
    {
        public TerminalMiddleware() : base(new MiddlewareSettings<TestMessage>(isMiddlewareEnabled: true)) { }

        public bool WasReached { get; private set; }

        public override ValueTask<TestMessage?> InvokeAsync(Envelope<TestMessage> envelope, CancellationToken cancellationToken = default)
        {
            WasReached = true;
            return ValueTask.FromResult<TestMessage?>(envelope.Message);
        }
    }

    #endregion

    [Fact]
    public void InvokeAsync_ForwardsToInnerMiddleware()
    {
        // Arrange
        var inner = new SpyMiddleware();
        var custom = new CustomMiddleware<TestMessage>(inner);
        var envelope = new Envelope<TestMessage>(new TestMessage(1, "test"));

        // Act
        custom.InvokeAsync(envelope).GetAwaiter().GetResult();

        // Assert
        Assert.True(inner.InvokeWasCalled);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue_WhenInnerIsEnabled()
    {
        // Arrange
        var inner = new SpyMiddleware { IsEnabled = true };

        // Act
        var custom = new CustomMiddleware<TestMessage>(inner);

        // Assert
        Assert.True(custom.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenInnerIsDisabled()
    {
        // Arrange
        var inner = new SpyMiddleware { IsEnabled = false };

        // Act
        var custom = new CustomMiddleware<TestMessage>(inner);

        // Assert
        Assert.False(custom.IsEnabled);
    }

    [Fact]
    public void IsDisabled_CausesMiddlewareToBeSkippedInChain()
    {
        // Arrange
        var invoker = new TestInvoker();
        var inner = new SpyMiddleware { IsEnabled = false };
        var custom = new CustomMiddleware<TestMessage>(inner);

        // Act
        invoker.AddMiddleware(custom);

        // Assert — disabled middleware is not added to chain, so Next is null
        Assert.Null(((IMiddleware<TestMessage>)invoker).Next);
    }

    [Fact]
    public void InvokeAsync_ContinuesChainToNextMiddleware()
    {
        // Arrange
        var invoker = new TestInvoker();
        var inner = new SpyMiddleware { IsEnabled = true };
        var custom = new CustomMiddleware<TestMessage>(inner);
        var terminal = new TerminalMiddleware();

        invoker.AddMiddleware(custom);
        invoker.AddMiddleware(terminal);

        var envelope = new Envelope<TestMessage>(new TestMessage(1, "test"));

        // Act
        invoker.InvokeAsync(envelope).GetAwaiter().GetResult();

        // Assert — inner was called and chain continued to terminal
        Assert.True(inner.InvokeWasCalled);
        Assert.True(terminal.WasReached);
    }
}
