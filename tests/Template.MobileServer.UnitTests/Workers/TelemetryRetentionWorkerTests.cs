namespace Template.MobileServer.Workers;

using Microsoft.Extensions.Logging.Abstractions;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Parameters;
using Template.MobileServer.Telemetry;
using Template.MobileServer.Web.Workers;

public sealed class TelemetryRetentionWorkerTests : IDisposable
{
    private const string TerminalId = "device-1";

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    [Fact]
    public async Task StartDeletesFileOfSilentDevice()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using (storage.BeginScope(DateTimeOffset.Now.AddDays(-40)))
        {
            await storage.Registry.EnsureRegisteredAsync(CreateInfo());
            storage.Registry.Apply(await storage.Store.SaveAsync(CreateBatch()));
        }

        var path = Path.Combine(storage.Root, TerminalId + ".db");
        Assert.True(File.Exists(path));
        using var worker = new TelemetryRetentionWorker(NullLogger<TelemetryRetentionWorker>.Instance, TimeProvider.System, new TelemetryRetentionWorkerOption(), storage.Store, storage.Registry);

        // Act
        await worker.StartAsync(TestContext.Current.CancellationToken);
        for (var i = 0; (i < 50) && File.Exists(path); i++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        await worker.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(File.Exists(path));
        var summary = Assert.Single(storage.Registry.Devices);
        Assert.Null(summary.Info);
        Assert.NotNull(await storage.DeviceService.QueryAsync(TerminalId));
    }

    private static TelemetryDeviceInfoEntity CreateInfo() =>
        new()
        {
            TerminalId = TerminalId,
            Manufacturer = "Google",
            Model = "Pixel 9a",
            OsName = "Android",
            OsVersion = "16",
            ServiceName = "Template.MobileApp",
            ServiceVersion = "1.0"
        };

    private static TelemetryBatch CreateBatch() =>
        new()
        {
            TerminalId = TerminalId,
            DeviceInfo = CreateInfo(),
            Resource = new TelemetryResourceEntity { Hash = "hash-1", ServiceInstanceId = "instance-1", ServiceVersion = "1.0", AttributesJson = "{}" }
        };
}
