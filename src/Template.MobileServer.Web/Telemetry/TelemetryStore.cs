namespace Template.MobileServer.Web.Telemetry;

using System.Collections.Concurrent;

using Template.MobileServer.Infrastructure.Telemetry;

// 端末ごとのテレメトリのファイルへの書き込み (保存・保持期間の削除・ファイルの削除) を端末ごとに 1 つずつにし、保存済みの Id を持つ
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

    // テレメトリのファイルがある端末
    public IReadOnlyList<string> EnumerateDevices() => Provider.EnumerateDevices();

    // 保存済みの Id は最初の書き込みで読み、新しく足した Id はコミットの後に足す
    public async ValueTask<TelemetrySaveResult> SaveAsync(TelemetryBatch batch)
    {
        var entry = GetEntry(batch.DeviceId);
        await entry.Lock.WaitAsync();
        try
        {
            if (!entry.Loaded)
            {
                var saved = await TelemetryService.QueryIdsAsync(batch.DeviceId);
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

    // 端末のファイルを削除する (ファイルが無ければ false)
    public async ValueTask<bool> DeleteDeviceAsync(string deviceId)
    {
        var entry = GetEntry(deviceId);
        await entry.Lock.WaitAsync();
        try
        {
            entry.Clear();
            return Provider.Delete(deviceId);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    // 保持期間の削除。ファイルごと削除したら保存済みの Id も捨てる
    public async ValueTask<TelemetryDeleteResult> DeleteExpiredAsync(string deviceId, long logBefore, long traceBefore, long metricBefore, long deviceBefore)
    {
        var entry = GetEntry(deviceId);
        await entry.Lock.WaitAsync();
        try
        {
            var result = await TelemetryService.DeleteExpiredAsync(deviceId, logBefore, traceBefore, metricBefore, deviceBefore);
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

    private Entry GetEntry(string deviceId) => entries.GetOrAdd(deviceId, static _ => new Entry());

    // 端末ごとの書き込みのロックと、保存済みの Resource (Hash → Id) と系列 (キー → Id)
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
