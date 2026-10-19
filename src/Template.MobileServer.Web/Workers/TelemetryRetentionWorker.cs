namespace Template.MobileServer.Web.Workers;

using Template.MobileServer.Web.Telemetry;

public sealed class TelemetryRetentionWorker : BackgroundService
{
    private const long NanosecondsPerMillisecond = 1_000_000;

    private ILogger<TelemetryRetentionWorker> Log { get; }

    private TimeProvider TimeProvider { get; }

    private TelemetryRetentionWorkerOption Options { get; }

    private TelemetryStore Store { get; }

    private TelemetryDeviceRegistry Registry { get; }

    public TelemetryRetentionWorker(
        ILogger<TelemetryRetentionWorker> log,
        TimeProvider timeProvider,
        TelemetryRetentionWorkerOption options,
        TelemetryStore store,
        TelemetryDeviceRegistry registry)
    {
        Log = log;
        TimeProvider = timeProvider;
        Options = options;
        Store = store;
        Registry = registry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Options.Enable)
        {
            Log.WarnWorkerDisabled(nameof(TelemetryRetentionWorker));
            return;
        }

        Log.InfoWorkerStart(nameof(TelemetryRetentionWorker));

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Options.IntervalMinutes), TimeProvider);
            do
            {
                await DeleteExpiredAsync();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            Log.InfoWorkerStop(nameof(TelemetryRetentionWorker));
        }
    }

    private async Task DeleteExpiredAsync()
    {
        var now = TimeProvider.GetUtcNow();
        var logBefore = ToUnixNano(now.AddDays(-Options.LogDays));
        var traceBefore = ToUnixNano(now.AddDays(-Options.TraceDays));
        var metricBefore = ToUnixNano(now.AddDays(-Options.MetricDays));
        var deviceBefore = ToUnixNano(now.AddDays(-Options.DeviceDays));

        foreach (var deviceId in Store.EnumerateDevices())
        {
            try
            {
                var result = await Store.DeleteExpiredAsync(deviceId, logBefore, traceBefore, metricBefore, deviceBefore);
                if (result.FileDeleted)
                {
                    Registry.ClearTelemetry(deviceId);
                    Log.InfoTelemetryFileDeleted(deviceId);
                }
                else if (result.Rows > 0)
                {
                    Log.InfoTelemetryExpired(deviceId, result.Rows);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.ErrorWorkerException(nameof(TelemetryRetentionWorker), ex);
            }
        }
    }

    private static long ToUnixNano(DateTimeOffset time) => time.ToUnixTimeMilliseconds() * NanosecondsPerMillisecond;
}
