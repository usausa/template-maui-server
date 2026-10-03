namespace Template.MobileServer.Web.Workers;

// 通知の保持期間の削除 (起動の直後と一定の間隔)。期限を過ぎた行は届いた / 届いていないにかかわらず削除する
public sealed class PushRetentionWorker : BackgroundService
{
    private readonly ILogger<PushRetentionWorker> log;

    private readonly TimeProvider timeProvider;

    private readonly PushRetentionWorkerOption options;

    private readonly PushService pushService;

    public PushRetentionWorker(
        ILogger<PushRetentionWorker> log,
        TimeProvider timeProvider,
        PushRetentionWorkerOption options,
        PushService pushService)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.options = options;
        this.pushService = pushService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enable)
        {
            log.InfoWorkerDisabled(nameof(PushRetentionWorker));
            return;
        }

        log.InfoWorkerStart(nameof(PushRetentionWorker));

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
            log.InfoWorkerStop(nameof(PushRetentionWorker));
        }
    }

    // 失敗はログに出して次の間隔でやり直す
    private async Task DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rows = await pushService.DeleteBeforeAsync(timeProvider.GetUtcNow().AddDays(-options.Days).UtcDateTime, cancellationToken);
            if (rows > 0)
            {
                log.InfoPushExpired(rows);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.ErrorWorkerException(nameof(PushRetentionWorker), ex);
        }
    }
}
