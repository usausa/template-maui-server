namespace Template.MobileServer.Workers;

using Microsoft.Extensions.Logging.Abstractions;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Telemetry;
using Template.MobileServer.Web.Workers;

public sealed class PushRetentionWorkerTests : IDisposable
{
    private const string DeviceId = "device-1";

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 起動の直後: 保持期間を過ぎた通知を削除し、期間内の通知は残す
    [Fact]
    public async Task StartDeletesExpiredMessages()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.Now;
        using (storage.BeginScope(now.AddDays(-8)))
        {
            await storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = DeviceId, Name = "Pixel 9a", IsEnabled = true }, cancellationToken);
            await storage.PushService.InsertAsync(DeviceId, "expired", string.Empty, cancellationToken);
        }

        PushMessageEntity current;
        using (storage.BeginScope(now))
        {
            current = Assert.Single(await storage.PushService.InsertAsync(DeviceId, "current", string.Empty, cancellationToken));
        }

        using var worker = new PushRetentionWorker(NullLogger<PushRetentionWorker>.Instance, TimeProvider.System, new PushRetentionWorkerOption(), storage.PushService);

        // Act
        await worker.StartAsync(cancellationToken);
        var pending = await storage.PushService.QueryPendingListAsync(DeviceId, cancellationToken);
        for (var i = 0; (i < 50) && (pending.Count > 1); i++)
        {
            await Task.Delay(100, cancellationToken);
            pending = await storage.PushService.QueryPendingListAsync(DeviceId, cancellationToken);
        }

        await worker.StopAsync(cancellationToken);

        // Assert
        Assert.Equal([current.Id], pending.Select(static x => x.Id));
    }
}
