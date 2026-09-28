namespace Template.MobileServer.Telemetry;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Enums;
using Template.MobileServer.Models.Parameters;

public sealed class TelemetryStoreTests : IDisposable
{
    private const string DeviceId = "device-1";

    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 保存済みの Id: 同じ Resource と系列は足さない。ファイルを削除したら Id を捨て、保存し直せる
    [Fact]
    public async Task SaveReusesIdsAndResetsAfterDelete()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var first = await storage.Store.SaveAsync(CreateBatch(1_000), cancellationToken);
        var second = await storage.Store.SaveAsync(CreateBatch(2_000), cancellationToken);
        var deleted = await storage.Store.DeleteDeviceAsync(DeviceId, cancellationToken);
        var third = await storage.Store.SaveAsync(CreateBatch(3_000), cancellationToken);

        // Assert
        Assert.Single(first.AddedResources);
        Assert.Single(first.AddedSeries);
        Assert.Empty(second.AddedResources);
        Assert.Empty(second.AddedSeries);
        Assert.Equal(first.AddedSeries[0].Id, Assert.Single(second.Metrics).Series.Id);
        Assert.True(deleted);
        Assert.Single(third.AddedResources);
        Assert.Single(third.AddedSeries);
        Assert.Equal(1L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM MetricPoint"));
    }

    // メモリ 1 点
    private static TelemetryBatch CreateBatch(long time)
    {
        var batch = new TelemetryBatch
        {
            DeviceId = DeviceId,
            DeviceInfo = new TelemetryDeviceInfoEntity
            {
                DeviceId = DeviceId,
                InstallationId = "installation-1",
                Manufacturer = "Google",
                Model = "Pixel 9a",
                OsName = "Android",
                OsVersion = "16",
                ServiceName = "Template.MobileApp",
                ServiceVersion = "1.0"
            },
            Resource = new TelemetryResourceEntity { Hash = "hash-1", ServiceInstanceId = "instance-1", ServiceVersion = "1.0", AttributesJson = "{}" }
        };

        var metric = new TelemetryMetric
        {
            Series = new TelemetryMetricSeriesEntity { Name = "process.memory.usage", ScopeName = "Template.MobileApp", Unit = "By", Kind = TelemetryMetricKind.Gauge, AttributesJson = "{}" }
        };
        metric.Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = time, Value = 100 });
        batch.Metrics.Add(metric);
        return batch;
    }
}
