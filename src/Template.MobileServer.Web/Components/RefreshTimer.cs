namespace Template.MobileServer.Web.Components;

// バスの通知をまとめて画面に反映する。通知の受け手は Request で予約するだけですぐ戻り、1 秒ごとに予約があれば refresh を呼ぶ (通知が続いても 1 秒に 1 回)。
// 予約が無くても interval ごとに呼ぶ (受信が途絶えた状態や時間軸を進めるため)。refresh は呼ぶ側で InvokeAsync の中で反映して描画する
public sealed class RefreshTimer : IDisposable
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

    private readonly CancellationTokenSource cts = new();

    private readonly TimeProvider timeProvider;

    private readonly Func<Task> refresh;

    private readonly int ticksPerInterval;

    private int requested;

    public RefreshTimer(TimeProvider timeProvider, TimeSpan interval, Func<Task> refresh)
    {
        this.timeProvider = timeProvider;
        this.refresh = refresh;
        ticksPerInterval = Math.Max(1, (int)(interval / Tick));
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
                    await refresh();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed
        }
        catch (ObjectDisposedException)
        {
            // The circuit is gone
        }
    }
}
