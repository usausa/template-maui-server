namespace Template.MobileServer.Web.Telemetry;

using System.Collections.Concurrent;

using Template.MobileServer.Infrastructure.Telemetry;

public sealed class TelemetryStore : IDisposable
{
    private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);

    private ITelemetryDbProvider Provider { get; }

    private TelemetryService TelemetryService { get; }

    public TelemetryStore(
        ITelemetryDbProvider provider,
        TelemetryService telemetryService)
    {
        Provider = provider;
        TelemetryService = telemetryService;
    }

    public void Dispose()
    {
        foreach (var entry in entries.Values)
        {
            entry.Dispose();
        }

        entries.Clear();
    }

    public IReadOnlyList<string> EnumerateDevices() => Provider.EnumerateDevices();

    public async ValueTask<TelemetrySaveResult> SaveAsync(TelemetryBatch batch)
    {
        var entry = GetEntry(batch.TerminalId);
        await entry.Lock.WaitAsync();
        try
        {
            if (!entry.Loaded)
            {
                var saved = await TelemetryService.QueryIdsAsync(batch.TerminalId);
                entry.Add(saved.Resources, saved.Series);
                entry.Loaded = true;
            }

            var result = await TelemetryService.SaveAsync(batch, entry.Resources, entry.Series);
            entry.Add(result.AddedResources, result.AddedSeries);
            return result;
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    public async ValueTask<bool> DeleteDeviceAsync(string terminalId)
    {
        var entry = GetEntry(terminalId);
        await entry.Lock.WaitAsync();
        try
        {
            entry.Clear();
            return Provider.Delete(terminalId);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    public async ValueTask<TelemetryDeleteResult> DeleteExpiredAsync(string terminalId, long logBefore, long traceBefore, long metricBefore, long deviceBefore)
    {
        var entry = GetEntry(terminalId);
        await entry.Lock.WaitAsync();
        try
        {
            var result = await TelemetryService.DeleteExpiredAsync(terminalId, logBefore, traceBefore, metricBefore, deviceBefore);
            if (result.FileDeleted)
            {
                entry.Clear();
            }

            return result;
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    private Entry GetEntry(string terminalId) => entries.GetOrAdd(terminalId, static _ => new Entry());

    private sealed class Entry : IDisposable
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public bool Loaded { get; set; }

        public Dictionary<string, long> Resources { get; } = [with(StringComparer.Ordinal)];

        public Dictionary<TelemetrySeriesKey, long> Series { get; } = [];

        public void Dispose() => Lock.Dispose();

        public void Add(IEnumerable<TelemetryResourceEntity> resources, IEnumerable<TelemetryMetricSeriesEntity> series)
        {
            foreach (var resource in resources)
            {
                Resources[resource.Hash] = resource.Id;
            }

            foreach (var entity in series)
            {
                Series[TelemetrySeriesKey.From(entity)] = entity.Id;
            }
        }

        public void Clear()
        {
            Loaded = false;
            Resources.Clear();
            Series.Clear();
        }
    }
}
