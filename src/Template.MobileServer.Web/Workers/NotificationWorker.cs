namespace Template.MobileServer.Web.Workers;

using Template.MobileServer.Web.Infrastructure.Notifications;

public sealed class NotificationWorker : BackgroundService
{
    private ILogger<NotificationWorker> Log { get; }

    private TimeProvider TimeProvider { get; }

    private NotificationWorkerOption Options { get; }

    private NotificationBus Bus { get; }

    public NotificationWorker(
        ILogger<NotificationWorker> log,
        TimeProvider timeProvider,
        NotificationWorkerOption options,
        NotificationBus bus)
    {
        Log = log;
        TimeProvider = timeProvider;
        Options = options;
        Bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Options.Enable)
        {
            Log.WarnWorkerDisabled(nameof(NotificationWorker));
            return;
        }

        Log.InfoWorkerStart(nameof(NotificationWorker));

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Options.IntervalSeconds), TimeProvider);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    Bus.Publish($"Server time: {TimeProvider.GetLocalNow():HH:mm:ss}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.ErrorWorkerException(nameof(NotificationWorker), ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            Log.InfoWorkerStop(nameof(NotificationWorker));
        }
    }
}
