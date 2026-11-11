namespace Template.MobileServer.Web.Components;

public sealed class RefreshTimer : IDisposable
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

    private readonly CancellationTokenSource cts;

    private readonly ILogger log;

    private readonly TimeProvider timeProvider;

    private readonly Func<Task> refresh;

    private readonly int ticksPerInterval;

    private int requested;

    public RefreshTimer(ILogger log, TimeProvider timeProvider, TimeSpan interval, Func<Task> refresh, CancellationToken stoppingToken)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.refresh = refresh;
        ticksPerInterval = Math.Max(1, (int)(interval / Tick));
        cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _ = RunAsync(cts.Token);
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }

    public void Request() => Interlocked.Exchange(ref requested, 1);

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(Tick, timeProvider);
            var ticks = 0;
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                ticks++;
                if ((Interlocked.Exchange(ref requested, 0) == 1) || (ticks >= ticksPerInterval))
                {
                    ticks = 0;
                    try
                    {
                        await refresh();
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException and not ObjectDisposedException)
                    {
                        log.ErrorRefreshFailed(ex);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed
        }
        catch (ObjectDisposedException)
        {
            // Disposed
        }
    }
}
