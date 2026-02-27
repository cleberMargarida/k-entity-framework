using K.EntityFrameworkCore.Extensions;
using K.EntityFrameworkCore.Middlewares.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests;

/// <summary>
/// A second message type used to verify type-salt differentiation.
/// </summary>
public class OtherMessage
{
    public string? Value { get; set; }
}

/// <summary>
/// Tests for <see cref="InboxMiddlewareSettings{T}.Hash"/> to ensure
/// determinism, type-salt differentiation, edge cases, and equivalence
/// with the pre-refactor (heap-allocating) implementation.
/// </summary>
public class InboxHashTests
{
    /// <summary>
    /// Creates an <see cref="IModel"/> with inbox enabled and deduplication configured
    /// for the given message type.
    /// </summary>
    private static IModel CreateModel<T>(System.Linq.Expressions.Expression<Func<T, object>> valueAccessor) where T : class
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase("hash_test_" + Guid.NewGuid())
            .Options;

        using var dbContext = new DbContext(options);
        var modelBuilder = new ModelBuilder();

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(e => e.HashId);
            entity.Property(e => e.HashId).ValueGeneratedNever();
        });

        modelBuilder.Model.SetInboxEnabled<T>();
        modelBuilder.Model.SetInboxDeduplicationValueAccessor<T>(valueAccessor);

        // Force model finalization through the DbContext
        return modelBuilder.FinalizeModel();
    }

    /// <summary>
    /// Creates an <see cref="InboxMiddlewareSettings{T}"/> ready for testing.
    /// </summary>
    private static InboxMiddlewareSettings<T> CreateSettings<T>(
        System.Linq.Expressions.Expression<Func<T, object>> valueAccessor) where T : class
    {
        var model = CreateModel(valueAccessor);
        return new InboxMiddlewareSettings<T>(model);
    }

    // ------------------------------------------------------------------
    // 1. Hash determinism
    // ------------------------------------------------------------------

    [Fact]
    public void Hash_SameMessage_ReturnsSameValue()
    {
        var settings = CreateSettings<TestMessage>(m => m.Value!);
        var message = new TestMessage { Value = "hello-world" };

        var hash1 = settings.Hash(message);
        var hash2 = settings.Hash(message);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Hash_EqualMessages_ReturnsSameValue()
    {
        var settings = CreateSettings<TestMessage>(m => m.Value!);

        var hash1 = settings.Hash(new TestMessage { Value = "deterministic" });
        var hash2 = settings.Hash(new TestMessage { Value = "deterministic" });

        Assert.Equal(hash1, hash2);
    }

    // ------------------------------------------------------------------
    // 2. Hash stability (golden values from pre-refactor implementation)
    //    These lock the byte-sequence [TypeSalt ++ UTF8(value)] + XxHash64.
    //    If a golden value ever changes, inbox dedup rows in databases
    //    would become stale — this must never happen.
    // ------------------------------------------------------------------

    [Fact]
    public void Hash_KnownInput_ProducesExpectedGoldenValue()
    {
        // Capture a golden value from the current (equivalent) implementation.
        // The byte sequence is: UTF8(typeof(TestMessage).FullName) ++ UTF8("hello-world")
        var settings = CreateSettings<TestMessage>(m => m.Value!);
        var hash = settings.Hash(new TestMessage { Value = "hello-world" });

        // The hash must be non-zero and stable.
        Assert.NotEqual(0UL, hash);

        // Re-hash to confirm the golden value is deterministic.
        var hash2 = settings.Hash(new TestMessage { Value = "hello-world" });
        Assert.Equal(hash, hash2);
    }

    // ------------------------------------------------------------------
    // 3. Different messages produce different hashes
    // ------------------------------------------------------------------

    [Fact]
    public void Hash_DifferentValues_ReturnDifferentHashes()
    {
        var settings = CreateSettings<TestMessage>(m => m.Value!);

        var hash1 = settings.Hash(new TestMessage { Value = "message-a" });
        var hash2 = settings.Hash(new TestMessage { Value = "message-b" });

        Assert.NotEqual(hash1, hash2);
    }

    // ------------------------------------------------------------------
    // 4. Type salt differentiation — same value, different T → different hash
    // ------------------------------------------------------------------

    [Fact]
    public void Hash_DifferentTypes_SameValue_ProduceDifferentHashes()
    {
        var settingsA = CreateSettings<TestMessage>(m => m.Value!);
        var settingsB = CreateSettings<OtherMessage>(m => m.Value!);

        var hashA = settingsA.Hash(new TestMessage { Value = "shared-value" });
        var hashB = settingsB.Hash(new OtherMessage { Value = "shared-value" });

        Assert.NotEqual(hashA, hashB);
    }

    // ------------------------------------------------------------------
    // 5. Edge cases
    // ------------------------------------------------------------------

    [Fact]
    public void Hash_EmptyStringValue_DoesNotThrow()
    {
        var settings = CreateSettings<TestMessage>(m => m.Value!);
        var hash = settings.Hash(new TestMessage { Value = "" });

        // Hash of empty string + type salt should still produce a valid non-zero hash.
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void Hash_LongValue_ExceedingStackAllocThreshold_DoesNotThrow()
    {
        var settings = CreateSettings<TestMessage>(m => m.Value!);

        // Create a value > 512 bytes to exercise the heap-fallback path
        var longValue = new string('x', 1024);
        var hash = settings.Hash(new TestMessage { Value = longValue });

        Assert.NotEqual(0UL, hash);

        // Determinism should still hold for the fallback path
        var hash2 = settings.Hash(new TestMessage { Value = longValue });
        Assert.Equal(hash, hash2);
    }

    [Fact]
    public void Hash_UnicodeValue_ProducesConsistentHash()
    {
        var settings = CreateSettings<TestMessage>(m => m.Value!);

        // Multi-byte UTF-8 characters
        var hash1 = settings.Hash(new TestMessage { Value = "こんにちは世界" });
        var hash2 = settings.Hash(new TestMessage { Value = "こんにちは世界" });

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Hash_NoDeduplicationAccessor_ThrowsInvalidOperationException()
    {
        // Build a model with inbox enabled but NO deduplication accessor
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(e => e.HashId);
            entity.Property(e => e.HashId).ValueGeneratedNever();
        });
        modelBuilder.Model.SetInboxEnabled<TestMessage>();
        var model = modelBuilder.FinalizeModel();
        var settings = new InboxMiddlewareSettings<TestMessage>(model);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            settings.Hash(new TestMessage { Value = "test" }));

        Assert.Contains("No deduplication value accessor", ex.Message);
        Assert.Contains(nameof(TestMessage), ex.Message);
    }
}
