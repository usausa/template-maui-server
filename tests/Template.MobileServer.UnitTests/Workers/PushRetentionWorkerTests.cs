namespace Template.MobileServer.Workers;

using Microsoft.Extensions.Logging.Abstractions;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Telemetry;
using Template.MobileServer.Web.Workers;

public sealed class PushRetentionWorkerTests : IDisposable
{
    private const string TerminalId = "device-1";

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    [Fact]
    public async Task StartDeletesExpiredMessages()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.Now;
        using (storage.BeginScope(now.AddDays(-8)))
        {
            await storage.DeviceService.InsertAsync(new DeviceEntity { TerminalId = TerminalId, Name = "Pixel 9a", IsEnabled = true });
            await storage.PushService.InsertAsync(TerminalId, "expired", string.Empty);
        }

        PushMessageEntity current;
        using (storage.BeginScope(now))
        {
            current = Assert.Single(await storage.PushService.InsertAsync(TerminalId, "current", string.Empty));
        }

        using var worker = new PushRetentionWorker(NullLogger<PushRetentionWorker>.Instance, TimeProvider.System, new PushRetentionWorkerOption(), storage.PushService);

        // Act
        await worker.StartAsync(cancellationToken);
        var pending = await storage.PushService.QueryPendingListAsync(TerminalId, cancellationToken);
        for (var i = 0; (i < 50) && (pending.Count > 1); i++)
        {
            await Task.Delay(100, cancellationToken);
            pending = await storage.PushService.QueryPendingListAsync(TerminalId, cancellationToken);
        }

        await worker.StopAsync(cancellationToken);

        // Assert
        Assert.Equal([current.Id], pending.Select(static x => x.Id));
    }
}
