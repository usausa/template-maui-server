namespace Template.MobileServer.Components;

using System.Runtime.CompilerServices;

using Template.MobileServer.Web.Components;

public sealed class RefreshTimerTests
{
    // 予約: 1 秒ごとの確認で反映する (続けて予約しても 1 回にまとめる)
    [Fact]
    public async Task RequestRefreshesOnNextTick()
    {
        // Arrange
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = new StrongBox<int>();
        using var timer = new RefreshTimer(TimeProvider.System, TimeSpan.FromMinutes(1), () =>
        {
            Interlocked.Increment(ref count.Value);
            refreshed.TrySetResult();
            return Task.CompletedTask;
        });

        // Act
        timer.Request();
        timer.Request();

        // Assert
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, Volatile.Read(ref count.Value));
    }

    // 間隔: 予約が無くても指定の間隔で反映する
    [Fact]
    public async Task IntervalRefreshesWithoutRequest()
    {
        // Arrange
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        using var timer = new RefreshTimer(TimeProvider.System, TimeSpan.FromSeconds(1), () =>
        {
            refreshed.TrySetResult();
            return Task.CompletedTask;
        });

        // Assert
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }
}
