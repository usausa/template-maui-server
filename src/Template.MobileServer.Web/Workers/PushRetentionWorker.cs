namespace Template.MobileServer.Web.Workers;

// 通知の保持期間の削除 (起動の直後と一定の間隔)。期限を過ぎた行は届いた / 届いていないにかかわらず削除する
public sealed class PushRetentionWorker : BackgroundService
{
    private ILogger<PushRetentionWorker> Log { get; }

    private TimeProvider TimeProvider { get; }

    private PushRetentionWorkerOption Options { get; }

    private PushService PushService { get; }

    public PushRetentionWorker(
        ILogger<PushRetentionWorker> log,
        TimeProvider timeProvider,
        PushRetentionWorkerOption options,
        PushService pushService)
    {
        Log = log;
        TimeProvider = timeProvider;
        Options = options;
        PushService = pushService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Options.Enable)
        {
            Log.WarnWorkerDisabled(nameof(PushRetentionWorker));
            return;
        }

        Log.InfoWorkerStart(nameof(PushRetentionWorker));

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
            Log.InfoWorkerStop(nameof(PushRetentionWorker));
        }
    }

    // 失敗はログに出して次の間隔でやり直す
    private async Task DeleteExpiredAsync()
    {
        try
        {
            var rows = await PushService.DeleteBeforeAsync(TimeProvider.GetUtcNow().AddDays(-Options.Days).UtcDateTime);
            if (rows > 0)
            {
                Log.InfoPushExpired(rows);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.ErrorWorkerException(nameof(PushRetentionWorker), ex);
        }
    }
}
