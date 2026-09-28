namespace Template.MobileServer.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Template.MobileServer.Domain;
using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Enums;
using Template.MobileServer.Models.Parameters;
using Template.MobileServer.Telemetry;
using Template.MobileServer.Web.Components.Pages;

public sealed class DashboardPageTests : MudBlazorTestBase
{
    // 表示: サマリは無効の端末を数えない (エラーは ERROR 以上なのでクラッシュを含む)。無効の端末の行は薄く、固有値は 0〜100 の横棒、直近のエラーは本文の 1 行目
    [Fact]
    public async Task RenderShowsSummaryAndDevices()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);

        // Act
        var cut = Render<DashboardPage>();

        // Assert
        Assert.Equal("1 / 1", cut.Find(".summary-devices").TextContent.Trim());
        Assert.Equal("2", cut.Find(".summary-errors").TextContent.Trim());
        Assert.Equal("1", cut.Find(".summary-crashes").TextContent.Trim());
        Assert.Equal("1", cut.Find(".summary-battery").TextContent.Trim());
        Assert.Equal(2, cut.FindAll("tr.device-row").Count);
        Assert.Contains("Reception", cut.Find("tr.device-disabled").TextContent, StringComparison.Ordinal);
        Assert.Contains("受信中", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("15%", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("width: 42%", cut.Markup, StringComparison.Ordinal);
        var errors = cut.FindAll("tr.error-row");
        Assert.Equal(2, errors.Count);
        Assert.Contains("crashed", errors[0].TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("at Main", cut.Markup, StringComparison.Ordinal);
    }

    // 検索: 端末 ID・名前・グループの部分一致で行を絞る
    [Fact]
    public async Task SearchFiltersDevices()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);
        var cut = Render<DashboardPage>();

        // Act
        await cut.Find(".search-field input").InputAsync(new ChangeEventArgs { Value = "1f" });

        // Assert
        var row = Assert.Single(cut.FindAll("tr.device-row"));
        Assert.Contains("Reception", row.TextContent, StringComparison.Ordinal);
    }

    // 通知: 登録の変更をバスで受けて読み直す
    [Fact]
    public async Task DeviceChangedRefreshesPage()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);
        var cut = Render<DashboardPage>();

        // Act
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            await storage.Registry.AddAsync(new DeviceEntity { DeviceId = "device-3", Name = "Added", IsEnabled = true }, Xunit.TestContext.Current.CancellationToken);
        }

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            Assert.Equal(3, cut.FindAll("tr.device-row").Count);
            Assert.Equal("1 / 2", cut.Find(".summary-devices").TextContent.Trim());
        }, TimeSpan.FromSeconds(5));
    }

    // 直近のエラー: 行を選ぶと、その端末のログ (ERROR 以上、エラーの時刻を含む範囲) へ
    [Fact]
    public async Task ErrorRowOpensLogs()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);
        var cut = Render<DashboardPage>();

        // Act
        await cut.Find("tr.error-row").ClickAsync(new MouseEventArgs());

        // Assert
        Assert.EndsWith("telemetry/device-1?tab=logs&range=15m&level=error", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    // 受信中の端末 (電池 15%、固有値 1 = 42、エラーとクラッシュ 1 件ずつ) と、グループ付きの無効の端末 (テレメトリなし)
    private async Task PrepareAsync(TelemetryTestStorage storage)
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        await storage.PrepareDatabaseAsync();
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            var batch = CreateBatch();
            await storage.Registry.EnsureRegisteredAsync(batch.DeviceInfo, cancellationToken);
            storage.Registry.Apply(await storage.Store.SaveAsync(batch, cancellationToken));
            await storage.Registry.AddAsync(new DeviceEntity { DeviceId = "device-2", Name = "Reception", GroupName = "1F", IsEnabled = false }, cancellationToken);
        }

        Services.AddSingleton(storage.Registry);
        Services.AddSingleton(storage.Bus);
    }

    private static TelemetryBatch CreateBatch()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
        var batch = new TelemetryBatch
        {
            DeviceId = "device-1",
            DeviceInfo = new TelemetryDeviceInfoEntity
            {
                DeviceId = "device-1",
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
            Series = new TelemetryMetricSeriesEntity { Name = "hw.battery.charge", ScopeName = "Template.MobileApp", Unit = "1", Kind = TelemetryMetricKind.Gauge, AttributesJson = "{}" }
        };
        metric.Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = now, Value = 0.15 });
        batch.Metrics.Add(metric);

        var custom = new TelemetryMetric
        {
            Series = new TelemetryMetricSeriesEntity { Name = "application.custom.value1", ScopeName = "Template.MobileApp", Unit = string.Empty, Kind = TelemetryMetricKind.Gauge, AttributesJson = "{}" }
        };
        custom.Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = now, Value = 42 });
        batch.Metrics.Add(custom);

        batch.Logs.Add(CreateLog(now - 1_000, TelemetrySeverity.Error, "failed"));
        batch.Logs.Add(CreateLog(now, TelemetrySeverity.Fatal, "crashed\nat Main"));
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
