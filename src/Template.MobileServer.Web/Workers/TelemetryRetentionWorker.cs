namespace Template.MobileServer.Web.Workers;

using Template.MobileServer.Web.Telemetry;

// テレメトリの保持期間の削除 (起動の直後と一定の間隔)。端末ごとに期限を過ぎた行を削除し、受信が途絶えた端末はファイルごと削除する (登録は残る)
public sealed class TelemetryRetentionWorker : BackgroundService
{
    private const long NanosecondsPerMillisecond = 1_000_000;

    private readonly ILogger<TelemetryRetentionWorker> log;

    private readonly TimeProvider timeProvider;

    private readonly TelemetryRetentionWorkerOption options;

    private readonly TelemetryService telemetryService;

    private readonly TelemetryDeviceRegistry registry;

    public TelemetryRetentionWorker(
        ILogger<TelemetryRetentionWorker> log,
        TimeProvider timeProvider,
        TelemetryRetentionWorkerOption options,
        TelemetryService telemetryService,
        TelemetryDeviceRegistry registry)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.options = options;
        this.telemetryService = telemetryService;
        this.registry = registry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enable)
        {
            log.InfoWorkerDisabled(nameof(TelemetryRetentionWorker));
            return;
        }

        log.InfoWorkerStart(nameof(TelemetryRetentionWorker));

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.IntervalMinutes), timeProvider);
            do
            {
                await DeleteExpiredAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            log.InfoWorkerStop(nameof(TelemetryRetentionWorker));
        }
    }

    // 1 台の失敗はログに出して次の端末へ進む
    private async Task DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var logBefore = ToUnixNano(now.AddDays(-options.LogDays));
        var traceBefore = ToUnixNano(now.AddDays(-options.TraceDays));
        var metricBefore = ToUnixNano(now.AddDays(-options.MetricDays));
        var deviceBefore = ToUnixNano(now.AddDays(-options.DeviceDays));

        foreach (var deviceId in telemetryService.EnumerateDevices())
        {
            try
            {
                var result = await telemetryService.DeleteExpiredAsync(deviceId, logBefore, traceBefore, metricBefore, deviceBefore, cancellationToken);
                if (result.FileDeleted)
                {
                    registry.ClearTelemetry(deviceId);
                    log.InfoTelemetryFileDeleted(deviceId);
                }
                else if (result.Rows > 0)
                {
                    log.InfoTelemetryExpired(deviceId, result.Rows);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.ErrorWorkerException(nameof(TelemetryRetentionWorker), ex);
            }
        }
    }

    private static long ToUnixNano(DateTimeOffset time) => time.ToUnixTimeMilliseconds() * NanosecondsPerMillisecond;
}
