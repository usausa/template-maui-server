namespace Template.MobileServer.Telemetry;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Enums;
using Template.MobileServer.Models.Parameters;
using Template.MobileServer.Services;

public sealed class TelemetryDeviceRegistryTests : IDisposable
{
    private const string DeviceId = "device-1";

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 自動登録: 未登録の端末を機種の名前で登録して通知し、無効にした端末は false
    [Fact]
    public async Task EnsureRegisteredRegistersUnknownDevice()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);
        var changed = new List<string>();
        storage.Bus.DeviceChanged += (_, e) => changed.Add(e.DeviceId);

        // Act / Assert: 登録
        Assert.True(await storage.Registry.EnsureRegisteredAsync(CreateInfo(), TestContext.Current.CancellationToken));
        var device = await storage.DeviceService.QueryAsync(DeviceId, TestContext.Current.CancellationToken);
        Assert.NotNull(device);
        Assert.Equal("Pixel 9a", device.Name);
        Assert.Equal([DeviceId], changed);

        // Act / Assert: 無効にする
        device.IsEnabled = false;
        Assert.Equal(DataWriteStatus.Success, await storage.Registry.UpdateAsync(device, TestContext.Current.CancellationToken));
        Assert.False(await storage.Registry.EnsureRegisteredAsync(CreateInfo(), TestContext.Current.CancellationToken));
    }

    // 要約: 保存した内容で最新値・エラーとクラッシュの件数・直近のエラー・受信の推移を更新する
    [Fact]
    public async Task ApplyUpdatesSummary()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);
        await storage.Registry.EnsureRegisteredAsync(CreateInfo(), TestContext.Current.CancellationToken);
        var result = await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);

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

    // 起動時の読み込み: 端末のファイルから要約を作り直し、登録の無いファイルは登録する
    [Fact]
    public async Task LoadRestoresSummaryAndRegistersFile()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);
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
        Assert.NotNull(await storage.DeviceService.QueryAsync(DeviceId, TestContext.Current.CancellationToken));
    }

    // 削除: 登録とテレメトリのファイルを消す
    [Fact]
    public async Task DeleteRemovesRegistrationAndFile()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);
        await storage.Registry.EnsureRegisteredAsync(CreateInfo(), TestContext.Current.CancellationToken);
        await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);

        // Act
        var status = await storage.Registry.DeleteAsync(DeviceId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(DataWriteStatus.Success, status);
        Assert.Empty(storage.Registry.Devices);
        Assert.Null(await storage.DeviceService.QueryAsync(DeviceId, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Path.Combine(storage.Root, DeviceId + ".db")));
    }

    // 端末からの登録: 未登録なら有効で登録し、登録済みなら名前だけを更新する (グループと有効はそのまま)
    [Fact]
    public async Task RegisterCreatesAndRenames()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(DateTimeOffset.Now);

        // Act / Assert: 登録
        var (device, created) = await storage.Registry.RegisterAsync(DeviceId, "受付", TestContext.Current.CancellationToken);
        Assert.True(created);
        Assert.True(device.IsEnabled);
        Assert.Equal("受付", device.Name);

        // Act / Assert: 管理画面でグループを付けて無効にした後、端末から名前を変える
        device.GroupName = "1F";
        device.IsEnabled = false;
        await storage.Registry.UpdateAsync(device, TestContext.Current.CancellationToken);
        (device, created) = await storage.Registry.RegisterAsync(DeviceId, "入口", TestContext.Current.CancellationToken);
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

    // メモリと固有値 1 の 1 点ずつ、エラー 1 件、クラッシュ 1 件 (時刻は今)
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
