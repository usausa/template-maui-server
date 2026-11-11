namespace Template.MobileServer.Components;

using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging.Abstractions;

using Template.MobileServer.Web.Components;

public sealed class RefreshTimerTests
{
    [Fact]
    public async Task RequestRefreshesOnNextTick()
    {
        // Arrange
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = new StrongBox<int>();
        using var timer = new RefreshTimer(NullLogger.Instance, TimeProvider.System, TimeSpan.FromMinutes(1), () =>
        {
            Interlocked.Increment(ref count.Value);
            refreshed.TrySetResult();
            return Task.CompletedTask;
        }, CancellationToken.None);

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
        using var timer = new RefreshTimer(NullLogger.Instance, TimeProvider.System, TimeSpan.FromSeconds(1), () =>
        {
            refreshed.TrySetResult();
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Assert
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RefreshContinuesAfterException()
    {
        // Arrange
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = new StrongBox<int>();

        // Act
        using var timer = new RefreshTimer(NullLogger.Instance, TimeProvider.System, TimeSpan.FromSeconds(1), () =>
        {
            if (Interlocked.Increment(ref count.Value) == 1)
            {
                return Task.FromException(new InvalidOperationException());
            }

            refreshed.TrySetResult();
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Assert
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(Volatile.Read(ref count.Value) >= 2);
    }

    [Fact]
    public async Task StoppingTokenStopsRefresh()
    {
        // Arrange
        using var stopping = new CancellationTokenSource();
        var count = new StrongBox<int>();
        using var timer = new RefreshTimer(NullLogger.Instance, TimeProvider.System, TimeSpan.FromSeconds(1), () =>
        {
            Interlocked.Increment(ref count.Value);
            return Task.CompletedTask;
        }, stopping.Token);

        // Act
        await stopping.CancelAsync();
        timer.Request();
        await Task.Delay(TimeSpan.FromSeconds(2.5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, Volatile.Read(ref count.Value));
    }
}
