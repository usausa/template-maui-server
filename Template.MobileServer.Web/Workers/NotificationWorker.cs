namespace Template.MobileServer.Web.Workers;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Infrastructure.Notifications;

public sealed class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> log;

    private readonly WorkerSetting setting;

    private readonly NotificationBus bus;

    private readonly TimeProvider timeProvider;

    public NotificationWorker(
        ILogger<NotificationWorker> log,
        WorkerSetting setting,
        NotificationBus bus,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.setting = setting;
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!setting.Enable)
        {
            log.InfoWorkerDisabled(nameof(NotificationWorker));
            return;
        }

        log.InfoWorkerStart(nameof(NotificationWorker));
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(setting.IntervalSeconds));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    bus.Publish($"Server time: {timeProvider.GetLocalNow():HH:mm:ss}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.ErrorUnhandledException(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            log.InfoWorkerStop(nameof(NotificationWorker));
        }
    }
}
