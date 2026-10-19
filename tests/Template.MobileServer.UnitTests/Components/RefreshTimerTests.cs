namespace Template.MobileServer.Components;

using System.Runtime.CompilerServices;

using Template.MobileServer.Web.Components;

public sealed class RefreshTimerTests
{
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
