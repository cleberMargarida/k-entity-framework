using K.EntityFrameworkCore.Diagnostics;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Diagnostics;

public class KafkaDiagnosticsTests : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _activities = [];
    private readonly Dictionary<string, long> _counterValues = [];
    private readonly List<double> _histogramValues = [];

    public KafkaDiagnosticsTests()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "K.EntityFrameworkCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "K.EntityFrameworkCore")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            _counterValues.TryGetValue(instrument.Name, out var current);
            _counterValues[instrument.Name] = current + measurement;
        });
        _meterListener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "k_ef.outbox.publish_duration")
            {
                _histogramValues.Add(measurement);
            }
        });
        _meterListener.Start();
    }

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        Assert.Equal("K.EntityFrameworkCore", KafkaDiagnostics.Source.Name);
    }

    [Fact]
    public void Meter_HasCorrectName()
    {
        Assert.Equal("K.EntityFrameworkCore", KafkaDiagnostics.Meter.Name);
    }

    [Fact]
    public void MessagesProduced_CounterIncrementsCorrectly()
    {
        KafkaDiagnostics.MessagesProduced.Add(1);
        _meterListener.RecordObservableInstruments();

        Assert.True(_counterValues.ContainsKey("k_ef.messages.produced"));
        Assert.True(_counterValues["k_ef.messages.produced"] >= 1);
    }

    [Fact]
    public void MessagesConsumed_CounterIncrementsCorrectly()
    {
        KafkaDiagnostics.MessagesConsumed.Add(1);
        _meterListener.RecordObservableInstruments();

        Assert.True(_counterValues.ContainsKey("k_ef.messages.consumed"));
        Assert.True(_counterValues["k_ef.messages.consumed"] >= 1);
    }

    [Fact]
    public void InboxDuplicatesFiltered_CounterIncrementsCorrectly()
    {
        KafkaDiagnostics.InboxDuplicatesFiltered.Add(1);
        _meterListener.RecordObservableInstruments();

        Assert.True(_counterValues.ContainsKey("k_ef.inbox.duplicates_filtered"));
        Assert.True(_counterValues["k_ef.inbox.duplicates_filtered"] >= 1);
    }

    [Fact]
    public void OutboxPublishDuration_HistogramRecordsCorrectly()
    {
        KafkaDiagnostics.OutboxPublishDuration.Record(42.5);
        _meterListener.RecordObservableInstruments();

        Assert.Contains(42.5, _histogramValues);
    }

    [Fact]
    public void ActivitySource_CreatesActivities()
    {
        using var activity = KafkaDiagnostics.Source.StartActivity("TestOperation");

        Assert.NotNull(activity);
        Assert.Equal("TestOperation", activity.DisplayName);
        Assert.Contains(_activities, a => a.DisplayName == "TestOperation");
    }

    [Fact]
    public void RegisterOutboxPendingGauge_ReportsValue()
    {
        int pendingCount = 42;
        KafkaDiagnostics.RegisterOutboxPendingGauge(() => pendingCount);

        var gaugeValues = new List<int>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "K.EntityFrameworkCore" && instrument.Name == "k_ef.outbox.pending")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((_, measurement, _, _) => gaugeValues.Add(measurement));
        listener.Start();
        listener.RecordObservableInstruments();

        Assert.Contains(42, gaugeValues);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }
}
