using K.EntityFrameworkCore.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Diagnostics;

public class TraceContextPropagatorTests : IDisposable
{
    private readonly ActivityListener _listener;

    public TraceContextPropagatorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "K.EntityFrameworkCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void Inject_WithNullActivity_ReturnsHeadersUnchanged()
    {
        var headers = ImmutableDictionary<string, string>.Empty.Add("existing", "value");

        var result = TraceContextPropagator.Inject(headers, activity: null);

        Assert.Equal(headers, result);
        Assert.False(result.ContainsKey("traceparent"));
    }

    [Fact]
    public void Inject_WithNullHeaders_ReturnsNewDictionaryWithTraceHeaders()
    {
        using var activity = KafkaDiagnostics.Source.StartActivity("Test", ActivityKind.Producer);
        Assert.NotNull(activity);

        var result = TraceContextPropagator.Inject(headers: null, activity);

        Assert.True(result.ContainsKey("traceparent"));
    }

    [Fact]
    public void Inject_WithActivity_AddsTraceparentHeader()
    {
        using var activity = KafkaDiagnostics.Source.StartActivity("Test", ActivityKind.Producer);
        Assert.NotNull(activity);
        var headers = ImmutableDictionary<string, string>.Empty;

        var result = TraceContextPropagator.Inject(headers, activity);

        Assert.True(result.ContainsKey("traceparent"));
        var traceparent = result["traceparent"];
        Assert.StartsWith("00-", traceparent);
        Assert.Contains(activity.TraceId.ToString(), traceparent);
        Assert.Contains(activity.SpanId.ToString(), traceparent);
    }

    [Fact]
    public void Inject_WithTraceState_AddsTracestateHeader()
    {
        using var activity = KafkaDiagnostics.Source.StartActivity("Test", ActivityKind.Producer);
        Assert.NotNull(activity);
        activity.TraceStateString = "vendor=value";
        var headers = ImmutableDictionary<string, string>.Empty;

        var result = TraceContextPropagator.Inject(headers, activity);

        Assert.True(result.ContainsKey("tracestate"));
        Assert.Equal("vendor=value", result["tracestate"]);
    }

    [Fact]
    public void Inject_PreservesExistingHeaders()
    {
        using var activity = KafkaDiagnostics.Source.StartActivity("Test", ActivityKind.Producer);
        Assert.NotNull(activity);
        var headers = ImmutableDictionary<string, string>.Empty
            .Add("custom-header", "custom-value");

        var result = TraceContextPropagator.Inject(headers, activity);

        Assert.Equal("custom-value", result["custom-header"]);
        Assert.True(result.ContainsKey("traceparent"));
    }

    [Fact]
    public void Extract_WithNullHeaders_ReturnsNull()
    {
        var result = TraceContextPropagator.Extract(headers: null);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_WithMissingTraceparent_ReturnsNull()
    {
        var headers = ImmutableDictionary<string, string>.Empty
            .Add("other", "value");

        var result = TraceContextPropagator.Extract(headers);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_WithInvalidTraceparent_ReturnsNull()
    {
        var headers = ImmutableDictionary<string, string>.Empty
            .Add("traceparent", "invalid-value");

        var result = TraceContextPropagator.Extract(headers);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_WithValidTraceparent_ReturnsActivityContext()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceparent = $"00-{traceId}-{spanId}-01";
        var headers = ImmutableDictionary<string, string>.Empty
            .Add("traceparent", traceparent);

        var result = TraceContextPropagator.Extract(headers);

        Assert.NotNull(result);
        Assert.Equal(traceId.ToString(), result.Value.TraceId.ToString());
        Assert.Equal(spanId.ToString(), result.Value.SpanId.ToString());
    }

    [Fact]
    public void Extract_WithTracestate_PreservesTraceState()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var headers = ImmutableDictionary<string, string>.Empty
            .Add("traceparent", $"00-{traceId}-{spanId}-01")
            .Add("tracestate", "vendor=value");

        var result = TraceContextPropagator.Extract(headers);

        Assert.NotNull(result);
        Assert.Equal("vendor=value", result.Value.TraceState);
    }

    [Fact]
    public void InjectAndExtract_Roundtrip_PreservesTraceId()
    {
        using var activity = KafkaDiagnostics.Source.StartActivity("Roundtrip", ActivityKind.Producer);
        Assert.NotNull(activity);
        var headers = ImmutableDictionary<string, string>.Empty;

        var injected = TraceContextPropagator.Inject(headers, activity);
        var extracted = TraceContextPropagator.Extract(injected);

        Assert.NotNull(extracted);
        Assert.Equal(activity.TraceId.ToString(), extracted.Value.TraceId.ToString());
    }

    [Fact]
    public void InjectAndExtract_Roundtrip_PreservesSpanId()
    {
        using var activity = KafkaDiagnostics.Source.StartActivity("Roundtrip", ActivityKind.Producer);
        Assert.NotNull(activity);
        var headers = ImmutableDictionary<string, string>.Empty;

        var injected = TraceContextPropagator.Inject(headers, activity);
        var extracted = TraceContextPropagator.Extract(injected);

        Assert.NotNull(extracted);
        Assert.Equal(activity.SpanId.ToString(), extracted.Value.SpanId.ToString());
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
