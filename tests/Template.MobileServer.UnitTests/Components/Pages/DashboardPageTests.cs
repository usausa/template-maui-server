namespace Template.MobileServer.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Template.MobileServer.Domain;
using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Parameters;
using Template.MobileServer.Telemetry;
using Template.MobileServer.Web.Components.Pages;
using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Services;

public sealed class DashboardPageTests : MudBlazorTestBase
{
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
        Assert.Equal("100%", cut.Find(".state-receiving").GetAttribute("width"));
        Assert.Equal("0%", cut.Find(".state-stopped").GetAttribute("width"));
        Assert.Equal("2", cut.Find(".summary-errors").TextContent.Trim());
        Assert.Equal("1", cut.Find(".summary-crashes").TextContent.Trim());
        Assert.Equal("1", cut.Find(".summary-battery").TextContent.Trim());
        Assert.Equal(2, cut.FindAll("tr.device-row").Count);
        Assert.Contains("Reception", cut.Find("tr.device-disabled").TextContent, StringComparison.Ordinal);
        Assert.Contains("受信中", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("15%", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("width=\"42%\"", cut.Markup, StringComparison.Ordinal);
        var errors = cut.FindAll("tr.error-row");
        Assert.Equal(2, errors.Count);
        Assert.Contains("crashed", errors[0].TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("at Main", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecentErrorsExcludeDisabledDevices()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            var device = await storage.DeviceService.QueryAsync("device-1");
            Assert.NotNull(device);
            device.IsEnabled = false;
            await storage.Registry.UpdateAsync(device);
        }

        // Act
        var cut = Render<DashboardPage>();

        // Assert
        Assert.Empty(cut.FindAll("tr.error-row"));
        Assert.Contains("直近 24 時間のエラーはありません。", cut.Markup, StringComparison.Ordinal);
    }

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
            await storage.Registry.AddAsync(new DeviceEntity { TerminalId = "device-3", Name = "Added", IsEnabled = true });
        }

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            Assert.Equal(3, cut.FindAll("tr.device-row").Count);
            Assert.Equal("1 / 2", cut.Find(".summary-devices").TextContent.Trim());
        }, TimeSpan.FromSeconds(5));
    }

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

    [Fact]
    public async Task PendingCountFollowsAcknowledge()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);
        var notifier = Services.GetRequiredService<PushNotifier>();
        List<PushMessageEntity> all;
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            all = await notifier.SendAsync(null, "件名", "本文");
            await notifier.SendAsync("device-1", "件名", "本文");
        }

        var cut = Render<DashboardPage>();
        Assert.Equal("2", cut.Find("tr.device-row .pending-cell").TextContent.Trim());
        Assert.True(cut.Find("tr.device-disabled .send-button").HasAttribute("disabled"));

        // Act
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            await notifier.AcknowledgeAsync("device-1", Assert.Single(all).Id);
        }

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Equal("1", cut.Find("tr.device-row .pending-cell").TextContent.Trim()), TimeSpan.FromSeconds(5));
    }

    private async Task PrepareAsync(TelemetryTestStorage storage)
    {
        await storage.PrepareDatabaseAsync();
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            var batch = CreateBatch();
            await storage.Registry.EnsureRegisteredAsync(batch.DeviceInfo);
            storage.Registry.Apply(await storage.Store.SaveAsync(batch));
            await storage.Registry.AddAsync(new DeviceEntity { TerminalId = "device-2", Name = "Reception", GroupName = "1F", IsEnabled = false });
        }

        Services.AddSingleton(storage.Registry);
        Services.AddSingleton(storage.Bus);
        Services.AddSingleton(storage.PushService);
        Services.AddSingleton(new PushNotifier(NullLogger<PushNotifier>.Instance, Substitute.For<IHubContext<PushHub, IPushClient>>(), storage.PushService));
    }

    private static TelemetryBatch CreateBatch()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
        var batch = new TelemetryBatch
        {
            TerminalId = "device-1",
            DeviceInfo = new TelemetryDeviceInfoEntity
            {
                TerminalId = "device-1",
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
