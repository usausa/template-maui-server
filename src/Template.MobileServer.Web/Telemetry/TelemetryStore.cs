namespace Template.MobileServer.Web.Telemetry;

using System.Collections.Concurrent;

using Template.MobileServer.Infrastructure.Telemetry;

// 端末ごとのテレメトリのファイルへの書き込み (保存・保持期間の削除・ファイルの削除) を端末ごとに 1 つずつにし、保存済みの Id を持つ
public sealed class TelemetryStore : IDisposable
{
    private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);

    private readonly ITelemetryDbProvider provider;

    private readonly TelemetryService telemetryService;

    public TelemetryStore(
        ITelemetryDbProvider provider,
        TelemetryService telemetryService)
    {
        this.provider = provider;
        this.telemetryService = telemetryService;
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
    public IReadOnlyList<string> EnumerateDevices() => provider.EnumerateDevices();

    public async ValueTask<TelemetrySaveResult> SaveAsync(TelemetryBatch batch, CancellationToken cancellationToken = default)
    {
        var entry = GetEntry(batch.DeviceId);
        await entry.Lock.WaitAsync(cancellationToken);
        try
        {
            return await telemetryService.SaveAsync(batch, entry.Ids, cancellationToken);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    // 端末のファイルを削除する (ファイルが無ければ false)
    public async ValueTask<bool> DeleteDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var entry = GetEntry(deviceId);
        await entry.Lock.WaitAsync(cancellationToken);
        try
        {
            entry.Ids.Clear();
            return provider.Delete(deviceId);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    // 保持期間の削除。ファイルごと削除したら保存済みの Id も捨てる
    public async ValueTask<TelemetryDeleteResult> DeleteExpiredAsync(string deviceId, long logBefore, long traceBefore, long metricBefore, long deviceBefore, CancellationToken cancellationToken = default)
    {
        var entry = GetEntry(deviceId);
        await entry.Lock.WaitAsync(cancellationToken);
        try
        {
            var result = await telemetryService.DeleteExpiredAsync(deviceId, logBefore, traceBefore, metricBefore, deviceBefore, cancellationToken);
            if (result.FileDeleted)
            {
                entry.Ids.Clear();
            }

            return result;
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    private Entry GetEntry(string deviceId) => entries.GetOrAdd(deviceId, static _ => new Entry());

    // 端末ごとの書き込みのロックと、保存済みの Resource と系列の Id
    private sealed class Entry : IDisposable
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public TelemetryIdCache Ids { get; } = new();

        public void Dispose() => Lock.Dispose();
    }
}
