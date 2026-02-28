using K.EntityFrameworkCore.Middlewares.Consumer;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Consumer;

/// <summary>
/// Tests for <see cref="SlidingWindow"/>.
/// </summary>
public class SlidingWindowTests
{
    [Fact]
    public void NewWindow_HasZeroFailures()
    {
        var window = new SlidingWindow(10);

        Assert.Equal(0, window.FailureCount);
    }

    [Fact]
    public void Record_Failures_IncreasesCount()
    {
        var window = new SlidingWindow(10);

        window.Record(true);
        window.Record(true);
        window.Record(true);

        Assert.Equal(3, window.FailureCount);
    }

    [Fact]
    public void Record_Successes_DoesNotIncreaseCount()
    {
        var window = new SlidingWindow(10);

        window.Record(false);
        window.Record(false);
        window.Record(false);

        Assert.Equal(0, window.FailureCount);
    }

    [Fact]
    public void Record_MixedOutcomes_TracksOnlyFailures()
    {
        var window = new SlidingWindow(10);

        window.Record(true);
        window.Record(false);
        window.Record(true);
        window.Record(false);
        window.Record(true);

        Assert.Equal(3, window.FailureCount);
    }

    [Fact]
    public void Record_WrapsAround_OldFailuresAreEvicted()
    {
        var window = new SlidingWindow(3);

        // Fill buffer: [fail, fail, fail]
        window.Record(true);
        window.Record(true);
        window.Record(true);
        Assert.Equal(3, window.FailureCount);

        // Overwrite first failure with success: [success, fail, fail]
        window.Record(false);
        Assert.Equal(2, window.FailureCount);

        // Overwrite second failure with success: [success, success, fail]
        window.Record(false);
        Assert.Equal(1, window.FailureCount);

        // Overwrite third failure with success: [success, success, success]
        window.Record(false);
        Assert.Equal(0, window.FailureCount);
    }

    [Fact]
    public void Record_WrapsAround_OldSuccessesAreEvicted()
    {
        var window = new SlidingWindow(3);

        // Fill buffer: [success, success, success]
        window.Record(false);
        window.Record(false);
        window.Record(false);
        Assert.Equal(0, window.FailureCount);

        // Overwrite with failures: [fail, success, success]
        window.Record(true);
        Assert.Equal(1, window.FailureCount);

        // [fail, fail, success]
        window.Record(true);
        Assert.Equal(2, window.FailureCount);

        // [fail, fail, fail]
        window.Record(true);
        Assert.Equal(3, window.FailureCount);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var window = new SlidingWindow(5);

        window.Record(true);
        window.Record(true);
        window.Record(true);
        Assert.Equal(3, window.FailureCount);

        window.Reset();

        Assert.Equal(0, window.FailureCount);
    }

    [Fact]
    public void Reset_AllowsRecordingAgain()
    {
        var window = new SlidingWindow(3);

        window.Record(true);
        window.Record(true);
        window.Reset();

        window.Record(true);
        Assert.Equal(1, window.FailureCount);
    }

    [Fact]
    public void Size_ReturnsConfiguredSize()
    {
        var window = new SlidingWindow(7);

        Assert.Equal(7, window.Size);
    }

    [Fact]
    public void SizeOne_TracksLastOutcomeOnly()
    {
        var window = new SlidingWindow(1);

        window.Record(true);
        Assert.Equal(1, window.FailureCount);

        window.Record(false);
        Assert.Equal(0, window.FailureCount);

        window.Record(true);
        Assert.Equal(1, window.FailureCount);
    }

    [Fact]
    public void AllFailures_FullWindow()
    {
        var window = new SlidingWindow(5);

        for (int i = 0; i < 5; i++)
            window.Record(true);

        Assert.Equal(5, window.FailureCount);
    }

    [Fact]
    public void AllSuccesses_FullWindow()
    {
        var window = new SlidingWindow(5);

        for (int i = 0; i < 5; i++)
            window.Record(false);

        Assert.Equal(0, window.FailureCount);
    }

    [Fact]
    public void Constructor_ThrowsForSizeLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingWindow(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingWindow(-1));
    }
}
