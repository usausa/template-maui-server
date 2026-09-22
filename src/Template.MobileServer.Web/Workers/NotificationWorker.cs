namespace Template.MobileServer.Web.Workers;

using Template.MobileServer.Web.Infrastructure.Notifications;

public sealed class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> log;

    private readonly NotificationWorkerOption options;

    private readonly NotificationBus bus;

    private readonly TimeProvider timeProvider;

    public NotificationWorker(
        ILogger<NotificationWorker> log,
        NotificationWorkerOption options,
        NotificationBus bus,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.options = options;
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enable)
        {
            log.InfoWorkerDisabled(nameof(NotificationWorker));
            return;
        }

        log.InfoWorkerStart(nameof(NotificationWorker));

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.IntervalSeconds));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    bus.Publish($"Server time: {timeProvider.GetLocalNow():HH:mm:ss}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.ErrorWorkerException(nameof(NotificationWorker), ex);
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
