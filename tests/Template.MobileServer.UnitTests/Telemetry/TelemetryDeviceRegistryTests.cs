namespace Template.MobileServer.Telemetry;

using Template.MobileServer.Domain;
using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Parameters;
using Template.MobileServer.Services;

public sealed class TelemetryDeviceRegistryTests : IDisposable
{
    private const string DeviceId = "device-1";

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    [Fact]
    public async Task EnsureRegisteredRegistersUnknownDevice()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);
        var changed = new List<string>();
        storage.Bus.DeviceChanged += (_, e) => changed.Add(e.DeviceId);

        // Act / Assert: 登録
        Assert.True(await storage.Registry.EnsureRegisteredAsync(CreateInfo()));
        var device = await storage.DeviceService.QueryAsync(DeviceId);
        Assert.NotNull(device);
        Assert.Equal("Pixel 9a", device.Name);
        Assert.Equal([DeviceId], changed);

        // Act / Assert: 無効にする
        device.IsEnabled = false;
        Assert.Equal(DataWriteStatus.Success, await storage.Registry.UpdateAsync(device));
        Assert.False(await storage.Registry.EnsureRegisteredAsync(CreateInfo()));
    }

    [Fact]
    public async Task ApplyUpdatesSummary()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);
        await storage.Registry.EnsureRegisteredAsync(CreateInfo());
        var result = await storage.Store.SaveAsync(CreateBatch());

        // Act
        storage.Registry.Apply(result);

        // Assert
        var summary = Assert.Single(storage.Registry.Devices);
        Assert.Equal("Pixel 9a", summary.Info?.Model);
        Assert.Equal(1024, summary.Memory?.Value);
        Assert.Equal(42, summary.CustomValue1?.Value);
        Assert.Null(summary.CustomValue2);
        Assert.Equal(2, summary.ErrorCount);
        Assert.Equal(1, summary.CrashCount);
        Assert.True(storage.Registry.IsReceiving(summary));
        Assert.Equal(["crashed", "failed"], storage.Registry.RecentErrors.Select(static x => x.Body));
        Assert.Equal(4, storage.Registry.IngestHistory.Sum());
    }

    [Fact]
    public async Task RecentErrorsAreWithinCountHours()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);
        await storage.Registry.EnsureRegisteredAsync(CreateInfo());
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
        var batch = CreateBatch();
        batch.Logs.Clear();
        batch.Logs.Add(CreateLog(now - (25 * 3_600_000_000_000), 17, "old"));
        batch.Logs.Add(CreateLog(now, 17, "new"));
        var result = await storage.Store.SaveAsync(batch);

        // Act
        storage.Registry.Apply(result);

        // Assert
        Assert.Equal(["new"], storage.Registry.RecentErrors.Select(static x => x.Body));
        Assert.Equal(1, Assert.Single(storage.Registry.Devices).ErrorCount);
    }

    [Fact]
    public async Task LoadRestoresSummaryAndRegistersFile()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            await storage.Store.SaveAsync(CreateBatch());
        }

        // Act
        await storage.Registry.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        var summary = Assert.Single(storage.Registry.Devices);
        Assert.Equal("Pixel 9a", summary.Device.Name);
        Assert.Equal(1024, summary.Memory?.Value);
        Assert.Equal(42, summary.CustomValue1?.Value);
        Assert.Equal(2, summary.ErrorCount);
        Assert.Equal(1, summary.CrashCount);
        Assert.NotNull(await storage.DeviceService.QueryAsync(DeviceId));
    }

    [Fact]
    public async Task DeleteRemovesRegistrationAndFile()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);
        await storage.Registry.EnsureRegisteredAsync(CreateInfo());
        await storage.Store.SaveAsync(CreateBatch());

        // Act
        var status = await storage.Registry.DeleteAsync(DeviceId);

        // Assert
        Assert.Equal(DataWriteStatus.Success, status);
        Assert.Empty(storage.Registry.Devices);
        Assert.Null(await storage.DeviceService.QueryAsync(DeviceId));
        Assert.False(File.Exists(Path.Combine(storage.Root, DeviceId + ".db")));
    }

    [Fact]
    public async Task RegisterCreatesAndRenames()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);

        // Act / Assert: 登録
        var (device, created) = await storage.Registry.RegisterAsync(DeviceId, "受付");
        Assert.True(created);
        Assert.True(device.IsEnabled);
        Assert.Equal("受付", device.Name);

        // Act / Assert: 管理画面でグループを付けて無効にした後、端末から名前を変える
        device.GroupName = "1F";
        device.IsEnabled = false;
        await storage.Registry.UpdateAsync(device);
        (device, created) = await storage.Registry.RegisterAsync(DeviceId, "入口");
        Assert.False(created);
        Assert.Equal("入口", device.Name);
        Assert.Equal("1F", device.GroupName);
        Assert.False(device.IsEnabled);
        Assert.Equal("入口", Assert.Single(storage.Registry.Devices).Device.Name);
    }

    private static TelemetryDeviceInfoEntity CreateInfo() =>
        new()
        {
            DeviceId = DeviceId,
            InstallationId = "installation-1",
            Manufacturer = "Google",
            Model = "Pixel 9a",
            OsName = "Android",
            OsVersion = "16",
            ServiceName = "Template.MobileApp",
            ServiceVersion = "1.0"
        };

    private static TelemetryBatch CreateBatch()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
        var batch = new TelemetryBatch
        {
            DeviceId = DeviceId,
            DeviceInfo = CreateInfo(),
            Resource = new TelemetryResourceEntity { Hash = "hash-1", ServiceInstanceId = "instance-1", ServiceVersion = "1.0", AttributesJson = "{}" }
        };

        var metric = new TelemetryMetric
        {
            Series = new TelemetryMetricSeriesEntity { Name = "process.memory.usage", ScopeName = "Template.MobileApp", Unit = "By", Kind = TelemetryMetricKind.Sum, Temporality = TelemetryTemporality.Cumulative, AttributesJson = "{}" }
        };
        metric.Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = now, Value = 1024 });
        batch.Metrics.Add(metric);

        var custom = new TelemetryMetric
        {
            Series = new TelemetryMetricSeriesEntity { Name = "application.custom.value1", ScopeName = "Template.MobileApp", Unit = string.Empty, Kind = TelemetryMetricKind.Gauge, AttributesJson = "{}" }
        };
        custom.Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = now, Value = 42 });
        batch.Metrics.Add(custom);

        batch.Logs.Add(CreateLog(now - 1_000, 17, "failed"));
        batch.Logs.Add(CreateLog(now, 21, "crashed\nat Main"));
        return batch;
    }

    private static TelemetryLogEntity CreateLog(long time, int severity, string body) =>
        new()
        {
            TimeUnixNano = time,
            ObservedTimeUnixNano = time,
            SeverityNumber = severity,
            SeverityText = string.Empty,
            EventName = string.Empty,
            Body = body,
            TraceId = string.Empty,
            SpanId = string.Empty,
            ScopeName = "Template.MobileApp",
            AttributesJson = "{}",
            Hash = time
        };
}
